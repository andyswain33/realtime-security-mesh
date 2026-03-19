using Gateway.Dashboard.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.Dashboard.Services
{
    // Example of how the Gateway.Processor (or a dedicated publisher) pushes the update:
    public class DashboardPublisher
    {
        private readonly IHubContext<SecurityHub, ISecurityDashboardClient> _hubContext;

        public DashboardPublisher(IHubContext<SecurityHub, ISecurityDashboardClient> hubContext)
            => _hubContext = hubContext;

        public async Task PublishOccupancyUpdateAsync(string zoneId, int newCount, string eventId)
        {
            // This ONLY pushes to clients who have explicitly called SubscribeToZone(zoneId)
            await _hubContext.Clients.Group(zoneId)
                             .ReceiveZoneUpdate(zoneId, newCount, eventId);
        }
    }
}
