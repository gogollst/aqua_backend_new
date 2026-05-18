using System.Text.Json;
using Aqua.Contracts;
using Aqua.Data.Tenancy;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NHibernate.Linq;

namespace Aqua.Data.Outbox;

/// <summary>
/// Periodically polls every tenant's <c>messaging_outbox</c> table and publishes pending rows.
/// Production tip: replace polling with logical-replication or notify trigger once the volume warrants it.
/// </summary>
public sealed class OutboxPublisherHostedService : BackgroundService
{
    private readonly SessionFactoryRegistry _registry;
    private readonly IBus _bus;
    private readonly ILogger<OutboxPublisherHostedService> _log;
    private readonly Func<IEnumerable<TenantId>> _tenantsProvider;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    public OutboxPublisherHostedService(
        SessionFactoryRegistry registry,
        IBus bus,
        ILogger<OutboxPublisherHostedService> log,
        Func<IEnumerable<TenantId>> tenantsProvider)
    {
        _registry = registry;
        _bus = bus;
        _log = log;
        _tenantsProvider = tenantsProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var tenant in _tenantsProvider())
            {
                try { await PublishPendingAsync(tenant, stoppingToken); }
                catch (Exception ex) { _log.LogError(ex, "Outbox publish loop failed for tenant {Tenant}", tenant); }
            }
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(TenantId tenant, CancellationToken ct)
    {
        var factory = _registry.GetFor(tenant);
        using var session = factory.OpenSession();
        using var tx = session.BeginTransaction();

        var pending = await session.Query<OutboxMessage>()
            .Where(x => x.DispatchedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        foreach (var row in pending)
        {
            try
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(row.HeadersJson) ?? new();
                await _bus.Publish(JsonSerializer.Deserialize<object>(row.Payload)!, ctx =>
                {
                    ctx.MessageId = row.Id;
                    foreach (var kv in headers) ctx.Headers.Set(kv.Key, kv.Value);
                }, ct);
                row.DispatchedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                row.Attempts++;
                row.LastError = ex.Message[..Math.Min(2000, ex.Message.Length)];
            }
            await session.UpdateAsync(row, ct);
        }
        await tx.CommitAsync(ct);
    }
}
