namespace Gateway.Dashboard.Hubs
{
    public interface ISecurityDashboardClient
    {
        // Strongly typed method for the client to listen to
        Task ReceiveZoneUpdate(string zoneId, int currentOccupancy, string lastEventId);
        Task ReceiveSecurityAlert(string message);
    }
}
