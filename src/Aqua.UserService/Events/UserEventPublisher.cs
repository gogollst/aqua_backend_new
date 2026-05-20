using Aqua.Contracts.Events;
using Aqua.Data.Outbox;

namespace Aqua.UserService.Events;

/// <summary>
/// Default publisher: wraps an <see cref="EventEnvelope{TData}"/> in an outbox-compatible
/// <see cref="IIntegrationEvent"/>, then defers persistence to <see cref="IOutboxWriter"/>.
///
/// MassTransit topology: events land on a single topic exchange (<see cref="ExchangeName"/>)
/// and the routing key (<c>user.created</c>, <c>role.updated</c>, etc.) is propagated through
/// the metadata headers so the outbox relay can bind it to the broker without per-event-type
/// CLR mappings. The real broker wiring is configured at compose/production startup; this class
/// is the only seam UserService needs.
/// </summary>
public sealed class UserEventPublisher : IUserEventPublisher
{
    /// <summary>Topic exchange every UserService event lands on.</summary>
    public const string ExchangeName = "aqua.user-service.events";

    /// <summary>Header key the outbox relay uses to read the AMQP routing key.</summary>
    public const string RoutingKeyHeader = "X-Aqua-Routing-Key";

    /// <summary>Header key the outbox relay uses to read the AMQP exchange name.</summary>
    public const string ExchangeHeader = "X-Aqua-Exchange";

    private readonly IOutboxWriter _outbox;
    private readonly Func<string> _correlationIdProvider;
    private readonly Func<string> _causationIdProvider;
    private readonly Func<Actor>  _actorProvider;

    public UserEventPublisher(
        IOutboxWriter outbox,
        Func<string> correlationIdProvider,
        Func<string> causationIdProvider,
        Func<Actor>  actorProvider)
    {
        _outbox                = outbox;
        _correlationIdProvider = correlationIdProvider;
        _causationIdProvider   = causationIdProvider;
        _actorProvider         = actorProvider;
    }

    public Task PublishAsync<TData>(long tenantId, string routingKey, TData data, CancellationToken ct = default)
        where TData : class
    {
        if (string.IsNullOrWhiteSpace(routingKey))
            throw new ArgumentException("Routing key must be a non-empty string.", nameof(routingKey));

        var envelope = EventEnvelope.Create(
            tenantId:      tenantId,
            correlationId: _correlationIdProvider(),
            causationId:   _causationIdProvider(),
            actor:         _actorProvider(),
            data:          data);

        var integrationEvent = new OutboxIntegrationEvent<TData>(envelope, ExchangeName, routingKey);
        return _outbox.WriteAsync(integrationEvent, ct);
    }
}

/// <summary>
/// Adapter that surfaces an <see cref="EventEnvelope{TData}"/> as an <see cref="IIntegrationEvent"/>
/// for the outbox. Exchange + routing-key live in <see cref="EventMetadata.Headers"/> so the relay
/// can read them without reflecting over <typeparamref name="TData"/>.
/// </summary>
public sealed record OutboxIntegrationEvent<TData> : IIntegrationEvent
    where TData : class
{
    public OutboxIntegrationEvent(EventEnvelope<TData> envelope, string exchangeName, string routingKey)
    {
        Envelope     = envelope;
        ExchangeName = exchangeName;
        RoutingKey   = routingKey;

        MessageId  = envelope.EventId;
        OccurredAt = new DateTimeOffset(DateTime.SpecifyKind(envelope.OccurredAt, DateTimeKind.Utc), TimeSpan.Zero);
        Metadata   = new EventMetadata
        {
            TenantId      = envelope.TenantId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            CorrelationId = envelope.CorrelationId,
        };
        if (envelope.Actor.UserId is { } uid)
            Metadata.OriginalUserId = uid.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Metadata.Headers[UserEventPublisher.ExchangeHeader]   = exchangeName;
        Metadata.Headers[UserEventPublisher.RoutingKeyHeader] = routingKey;
        Metadata.Headers["X-Aqua-Causation-Id"]               = envelope.CausationId;
        Metadata.Headers["X-Aqua-Event-Type"]                 = envelope.EventType;
    }

    public EventEnvelope<TData> Envelope     { get; }
    public string               ExchangeName { get; }
    public string               RoutingKey   { get; }

    public Guid             MessageId  { get; }
    public DateTimeOffset   OccurredAt { get; }
    public EventMetadata    Metadata   { get; }
}
