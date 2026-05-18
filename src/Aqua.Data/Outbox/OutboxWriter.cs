using System.Text.Json;
using Aqua.Contracts.Events;
using Aqua.Data.Sessions;
using Aqua.Data.Tenancy;

namespace Aqua.Data.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly ISessionScope _scope;
    private readonly ITenantContext _tenant;

    public OutboxWriter(ISessionScope scope, ITenantContext tenant)
    {
        _scope = scope;
        _tenant = tenant;
    }

    public async Task WriteAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent
    {
        var row = new OutboxMessage
        {
            TenantId = _tenant.Current?.Value ?? "",
            MessageType = typeof(TEvent).FullName!,
            Payload = JsonSerializer.Serialize<object>(@event),
            HeadersJson = JsonSerializer.Serialize(@event.Metadata.Headers),
        };
        await _scope.Session.SaveAsync(row, ct);
    }
}
