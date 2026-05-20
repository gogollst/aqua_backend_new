using System.Text.Json;
using Aqua.Contracts.Events;
using Aqua.Data.Outbox;
using Aqua.UserService.Infrastructure;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Events;

/// <summary>
/// UserService-local <see cref="IOutboxWriter"/>. Persists outbox rows via the per-request
/// NHibernate <see cref="ISession"/> and pulls tenant id from <see cref="ICurrentTenant"/>.
///
/// We don't reuse the shared <c>Aqua.Data.Outbox.OutboxWriter</c> because that one binds to
/// <c>ISessionScope</c> / <c>ITenantContext</c> from <c>Aqua.Data</c>, which UserService does not
/// register (UserService brings its own scoped <see cref="ISession"/> + <see cref="ICurrentTenant"/>).
/// The on-disk row layout is identical, so the same outbox relay can drain both services.
/// </summary>
public sealed class UserServiceOutboxWriter : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly ISession _session;
    private readonly ICurrentTenant _tenant;

    public UserServiceOutboxWriter(ISession session, ICurrentTenant tenant)
    {
        _session = session;
        _tenant = tenant;
    }

    public async Task WriteAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var row = new OutboxMessage
        {
            TenantId    = _tenant.Id?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            MessageType = typeof(TEvent).FullName!,
            Payload     = JsonSerializer.Serialize<object>(@event, JsonOpts),
            HeadersJson = JsonSerializer.Serialize(@event.Metadata.Headers, JsonOpts),
        };
        await _session.SaveAsync(row, ct);
    }
}
