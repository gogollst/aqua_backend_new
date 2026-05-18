using Aqua.Contracts;
using Aqua.Contracts.Events;
using Aqua.Data.DependencyInjection;
using Aqua.Data.Outbox;
using Aqua.Data.Sessions;
using Aqua.Data.Tenancy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Aqua.Data.Tests;

public sealed record SampleHappenedEvent(string Payload) : IntegrationEventBase;

public class OutboxInboxIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    private IServiceProvider _sp = default!;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        // Create schema using the connection. We run a single CREATE TABLE for the outbox table; the
        // Aqua.Data mapping uses hbm2ddl.auto=validate so the table must exist beforehand.
        await using var conn = new Npgsql.NpgsqlConnection(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE messaging_outbox (
              id            UUID PRIMARY KEY,
              tenant_id     VARCHAR(64) NOT NULL,
              message_type  VARCHAR(256) NOT NULL,
              payload       TEXT NOT NULL,
              headers_json  TEXT NOT NULL,
              created_at    TIMESTAMPTZ NOT NULL,
              dispatched_at TIMESTAMPTZ NULL,
              attempts      INT NOT NULL,
              last_error    VARCHAR(2000) NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAquaData(opts =>
        {
            opts.ResolveTenantConfig = _ => new TenantDbConfig(SupportedDbms.Postgres, _pg.GetConnectionString());
        });

        _sp = services.BuildServiceProvider();
    }

    public Task DisposeAsync() => _pg.DisposeAsync().AsTask();

    [Fact(Skip = "Docker not available in this environment — run manually when Docker Desktop is running")]
    public async Task OutboxWrite_PersistsRowInSameSession()
    {
        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().Set(new TenantId("acme"));

        var sessionScope = scope.ServiceProvider.GetRequiredService<ISessionScope>();
        var writer = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();

        using var tx = sessionScope.Session.BeginTransaction();
        await writer.WriteAsync(new SampleHappenedEvent("hello"));
        await tx.CommitAsync();

        var count = sessionScope.Session.CreateSQLQuery("SELECT COUNT(*) FROM messaging_outbox")
            .UniqueResult<long>();
        count.Should().Be(1);
    }
}
