namespace Aqua.Contracts.Events;

/// <summary>
/// Marker for events sent across service boundaries via the message bus.
/// </summary>
public interface IIntegrationEvent
{
    Guid MessageId { get; }
    DateTimeOffset OccurredAt { get; }
    EventMetadata Metadata { get; }
}
