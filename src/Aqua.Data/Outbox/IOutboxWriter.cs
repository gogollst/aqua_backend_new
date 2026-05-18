using Aqua.Contracts.Events;

namespace Aqua.Data.Outbox;

public interface IOutboxWriter
{
    Task WriteAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent;
}
