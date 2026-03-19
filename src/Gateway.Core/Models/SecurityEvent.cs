namespace Gateway.Core.Models;

public record SecurityEvent
{
    public required Guid EventId { get; init; }
    public required string ZoneId { get; init; }
    public required string DeviceId { get; init; }
    public required EventType Type { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    // Optional payload data, e.g., the number of people who just walked through
    public int? OccupancyDelta { get; init; }
}

public enum EventType
{
    PeopleCountUpdate,
    DoorForcedOpen,
    DoorHeldOpen,
    AccessGranted,
    AccessDenied
}