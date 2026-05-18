namespace Aqua.Contracts.Events;

/// <summary>
/// Base record for integration events; auto-populates MessageId and OccurredAt.
/// Use as <c>public sealed record TestCaseCreated(...) : IntegrationEventBase;</c>.
/// </summary>
public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public EventMetadata Metadata { get; init; } = new();
}
