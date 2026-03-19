using System.Text.Json;
using Gateway.Core.Models;
using Gateway.Core.Security;
using Gateway.Ingress.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Dependencies
builder.Services.AddSingleton<PayloadSanitizer>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

var app = builder.Build();

// 2. Define the High-Speed Ingress Endpoint
app.MapPost("/api/events/telemetry", async (
    HttpContext context,
    [FromServices] PayloadSanitizer sanitizer,
    [FromServices] IRabbitMqPublisher publisher) =>
{
    // A. Read the raw payload BEFORE parsing to prevent parser exploitation
    using var reader = new StreamReader(context.Request.Body);
    var rawPayload = await reader.ReadToEndAsync();

    // B. Run the AST validation
    if (!sanitizer.IsSafeTelemetry(rawPayload))
    {
        // Log the security breach attempt here
        return Results.BadRequest(new { error = "Malicious payload structure detected." });
    }

    // C. Safely deserialize the validated payload
    SecurityEvent? securityEvent;
    try
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        securityEvent = JsonSerializer.Deserialize<SecurityEvent>(rawPayload, options);

        if (securityEvent is null) return Results.BadRequest(new { error = "Empty payload." });
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { error = "Invalid telemetry schema." });
    }

    // D. Drop the message onto the RabbitMQ exchange
    await publisher.PublishEventAsync(securityEvent);

    // E. Release the connection back to the edge device immediately
    return Results.Accepted();
});

app.Run();