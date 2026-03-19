using Gateway.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Gateway.Processor.Services;

public class DashboardForwarder
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<DashboardForwarder> _logger;

    public DashboardForwarder(IConfiguration configuration, ILogger<DashboardForwarder> logger)
    {
        _logger = logger;

        // In Docker, this will point to the Dashboard container. Locally, it points to the localhost port.
        var dashboardUrl = configuration["DashboardUrl"] ?? "http://localhost:5003/securityHub";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(dashboardUrl)
            .WithAutomaticReconnect() // Crucial for resilient microservices
            .Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("Successfully connected to the SignalR Dashboard.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initially connect to Dashboard. Will retry automatically.");
        }
    }

    public async Task BroadcastEventAsync(SecurityEvent securityEvent)
    {
        if (_hubConnection.State != HubConnectionState.Connected) return;

        // Route the event to the appropriate SignalR Hub method based on the EventType
        if (securityEvent.Type == EventType.PeopleCountUpdate && securityEvent.OccupancyDelta.HasValue)
        {
            // Pushes to the "Group" pattern we defined in the Dashboard
            await _hubConnection.InvokeAsync("PublishOccupancyUpdateAsync",
                securityEvent.ZoneId,
                securityEvent.OccupancyDelta.Value,
                securityEvent.EventId.ToString());
        }
        else if (securityEvent.Type == EventType.DoorForcedOpen)
        {
            await _hubConnection.InvokeAsync("PublishSecurityAlertAsync",
                securityEvent.ZoneId,
                $"DOOR FORCED ALARM - Device: {securityEvent.DeviceId}");
        }
    }
}