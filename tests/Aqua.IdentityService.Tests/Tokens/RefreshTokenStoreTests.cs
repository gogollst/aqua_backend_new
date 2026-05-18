using Aqua.Contracts;
using Aqua.Data.DependencyInjection;
using Aqua.Data.Sessions;
using Aqua.Data.Tenancy;
using Aqua.IdentityService.Domain;
using Aqua.IdentityService.Tokens;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Aqua.IdentityService.Tests.Tokens;

public class RefreshTokenStoreTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    private IServiceProvider _sp = default!;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        await using var conn = new Npgsql.NpgsqlConnection(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE auth_refresh_token (
              id UUID PRIMARY KEY,
              user_id INT NOT NULL,
              tenant_id VARCHAR(64) NOT NULL,
              token_hash VARCHAR(128) NOT NULL UNIQUE,
              issued_at TIMESTAMPTZ NOT NULL,
              expires_at TIMESTAMPTZ NOT NULL,
              rotated_to_token_id UUID,
              revoked_at TIMESTAMPTZ,
              revocation_reason VARCHAR(200),
              client_ip VARCHAR(64)
            );
            """;
        await cmd.ExecuteNonQueryAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAquaData(opts =>
        {
            opts.ResolveTenantConfig = _ => new TenantDbConfig(SupportedDbms.Postgres, _pg.GetConnectionString());
        }, RefreshTokenMapping.Apply);

        _sp = services.BuildServiceProvider();
    }

    public Task DisposeAsync() => _pg.DisposeAsync().AsTask();

    [Fact(Skip = "Docker not available in this environment — run manually when Docker Desktop is running")]
    public async Task SaveAndConsume_RotatesToken()
    {
        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().Set(new TenantId("acme"));

        var sessionScope = scope.ServiceProvider.GetRequiredService<ISessionScope>();
        var store = new RefreshTokenStore(sessionScope);

        using var tx = sessionScope.Session.BeginTransaction();
        const string token = "test-refresh-token-abc";
        await store.SaveAsync(token, userId: 1, tenantId: "acme", expiresAt: DateTimeOffset.UtcNow.AddDays(14), clientIp: null);
        await tx.CommitAsync();

        using var tx2 = sessionScope.Session.BeginTransaction();
        var newId = Guid.NewGuid();
        var consumed = await store.ConsumeAsync(token, newId);
        await tx2.CommitAsync();

        consumed.Should().NotBeNull();
        consumed!.RotatedToTokenId.Should().Be(newId);
    }

    [Fact(Skip = "Docker not available in this environment — run manually when Docker Desktop is running")]
    public async Task ConsumeSecondTime_DetectsReuse_ReturnsNull()
    {
        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().Set(new TenantId("acme"));

        var sessionScope = scope.ServiceProvider.GetRequiredService<ISessionScope>();
        var store = new RefreshTokenStore(sessionScope);

        const string token = "reuse-test-token-xyz";
        using var tx = sessionScope.Session.BeginTransaction();
        await store.SaveAsync(token, userId: 2, tenantId: "acme", expiresAt: DateTimeOffset.UtcNow.AddDays(14), clientIp: null);
        await tx.CommitAsync();

        using var tx2 = sessionScope.Session.BeginTransaction();
        var consumed1 = await store.ConsumeAsync(token, Guid.NewGuid());
        await tx2.CommitAsync();
        consumed1.Should().NotBeNull();

        // Second consume on same original token → reuse detected
        using var tx3 = sessionScope.Session.BeginTransaction();
        var consumed2 = await store.ConsumeAsync(token, Guid.NewGuid());
        await tx3.CommitAsync();
        consumed2.Should().BeNull();
    }
}
