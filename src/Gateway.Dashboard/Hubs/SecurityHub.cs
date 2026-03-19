using Microsoft.AspNetCore.SignalR;

namespace Gateway.Dashboard.Hubs
{
    public class SecurityHub : Hub<ISecurityDashboardClient>
    {
        // 1. Called by the web browser when a user selects a zone
        public async Task SubscribeToZone(string zoneId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, zoneId);
            await Clients.Caller.ReceiveSecurityAlert($"Successfully subscribed to telemetry for Zone: {zoneId}");
        }

        public async Task UnsubscribeFromZone(string zoneId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, zoneId);
        }

        // 2. Called by the Gateway.Processor to push standard telemetry to the subscribed browsers
        public async Task PublishOccupancyUpdateAsync(string zoneId, int currentOccupancy, string eventId)
        {
            await Clients.Group(zoneId).ReceiveZoneUpdate(zoneId, currentOccupancy, eventId);
        }

        // 3. Called by the Gateway.Processor to push ALARMS to the subscribed browsers
        public async Task PublishSecurityAlertAsync(string zoneId, string message)
        {
            await Clients.Group(zoneId).ReceiveSecurityAlert(message);
        }
    }
}
