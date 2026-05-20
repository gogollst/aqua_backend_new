namespace Aqua.UserService.Events;

/// <summary>
/// Publishes UserService domain events to the outbox (which a background relay forwards
/// to the message bus). All events flow through a single topic exchange named
/// <see cref="UserEventPublisher.ExchangeName"/>; the <c>routingKey</c> argument selects the
/// concrete event type (e.g. <c>"user.created"</c>, <c>"role.updated"</c>).
/// </summary>
public interface IUserEventPublisher
{
    Task PublishAsync<TData>(long tenantId, string routingKey, TData data, CancellationToken ct = default)
        where TData : class;
}
