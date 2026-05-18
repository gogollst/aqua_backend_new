namespace Aqua.Data.Outbox;

/// <summary>
/// Persisted in the same transaction as the domain change. A background relay
/// publishes pending rows to the message bus.
/// </summary>
public class OutboxMessage
{
    public virtual Guid Id { get; protected set; } = Guid.NewGuid();
    public virtual string TenantId { get; set; } = default!;
    public virtual string MessageType { get; set; } = default!;
    public virtual string Payload { get; set; } = default!;          // JSON
    public virtual string HeadersJson { get; set; } = "{}";
    public virtual DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public virtual DateTimeOffset? DispatchedAt { get; set; }
    public virtual int Attempts { get; set; }
    public virtual string? LastError { get; set; }
}
