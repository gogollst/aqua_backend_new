namespace Aqua.UserService.Events;

public sealed record EventEnvelope<TData>(
    Guid EventId,
    string EventType,
    int EventVersion,
    DateTime OccurredAt,
    long TenantId,
    string CorrelationId,
    string CausationId,
    Actor Actor,
    TData Data);

public sealed record Actor(string Type, long? UserId = null);

public static class EventEnvelope
{
    public static EventEnvelope<TData> Create<TData>(
        long tenantId, string correlationId, string causationId,
        Actor actor, TData data, int version = 1) where TData : class =>
        new(
            EventId:       Guid.NewGuid(),
            EventType:     typeof(TData).Name,
            EventVersion:  version,
            OccurredAt:    DateTime.UtcNow,
            TenantId:      tenantId,
            CorrelationId: correlationId,
            CausationId:   causationId,
            Actor:         actor,
            Data:          data);
}
