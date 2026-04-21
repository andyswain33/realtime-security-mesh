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

// Static JsonSerializerOptions for high performance
var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var logger = app.Services.GetRequiredService<ILogger<Program>>();

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
        // Log the security breach attempt internally - don't expose details to client
        logger.LogWarning("Malicious payload structure detected from {RemoteIP}", context.Connection.RemoteIpAddress);
        return Results.BadRequest();
    }

    // C. Safely deserialize the validated payload
    SecurityEvent? securityEvent;
    try
    {
        securityEvent = JsonSerializer.Deserialize<SecurityEvent>(rawPayload, jsonOptions);

        if (securityEvent is null) return Results.BadRequest();
    }
    catch (JsonException)
    {
        return Results.BadRequest();
    }

    // D. Drop the message onto the RabbitMQ exchange
    await publisher.PublishEventAsync(securityEvent);

    // E. Release the connection back to the edge device immediately
    return Results.Accepted();
});

app.Run();