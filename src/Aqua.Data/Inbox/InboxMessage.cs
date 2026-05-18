namespace Aqua.Data.Inbox;

/// <summary>
/// Persisted message-id of every successfully processed inbound message.
/// Used to deduplicate (idempotency) at-least-once delivery from the bus.
/// </summary>
public class InboxMessage
{
    public virtual Guid Id { get; protected set; } = default!;            // = MassTransit MessageId
    public virtual string TenantId { get; set; } = default!;
    public virtual string Consumer { get; set; } = default!;             // ConsumerType.FullName
    public virtual DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
