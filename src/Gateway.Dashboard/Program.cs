using Gateway.Dashboard.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR Services
builder.Services.AddSignalR();

// Configure CORS so a separate frontend (React/Angular) can connect to the Hub
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Standard React/Vite ports
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Credentials are required for SignalR WebSockets
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// Map the Security Hub to a specific endpoint
app.MapHub<SecurityHub>("/securityHub");

// Health check endpoint
app.MapGet("/", () => "Gateway Dashboard Active. SignalR Hub running at /securityHub");

// Force the app to run on port 5003 so the Processor knows exactly where to find it
app.Run("http://localhost:5003");