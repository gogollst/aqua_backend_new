using Aqua.IdentityService.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Testcontainers.PostgreSql;
using Xunit;

namespace Aqua.IdentityService.Tests.Integration;

public sealed class FullLoginFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    private string _privPath = default!, _pubPath = default!;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        // CREATE aquaUser + auth_refresh_token
        await using var conn = new Npgsql.NpgsqlConnection(_pg.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE SEQUENCE "AQUAUSER_ID_SEQ" START 1;
            CREATE TABLE "aquaUser" (
              "Id" INT PRIMARY KEY DEFAULT nextval('"AQUAUSER_ID_SEQ"'),
              "UserName" VARCHAR(50) NOT NULL UNIQUE,
              "FirstName" VARCHAR(100), "Surname" VARCHAR(100),
              "Email" VARCHAR(100), "LastLogin" TIMESTAMP,
              "Phone" VARCHAR(50), "Position" VARCHAR(100),
              "UserStatus" INT NOT NULL DEFAULT 0,
              "Deleted" BOOLEAN NOT NULL DEFAULT FALSE,
              "ServerAdmin" BOOLEAN NOT NULL DEFAULT FALSE,
              "PasswordExpiryExempt" BOOLEAN NOT NULL DEFAULT FALSE,
              "ClearTextPassword" VARCHAR(100),
              "Password" VARCHAR(172),
              "LastPasswordChange" TIMESTAMP,
              "PasswordHistory" TEXT,
              "FailedLoginCount" INT NOT NULL DEFAULT 0,
              "LockedUntil" TIMESTAMP
            );
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
            INSERT INTO "aquaUser" ("UserName", "Password") VALUES ('alice', @hash);
            """;
        cmd.Parameters.AddWithValue("hash", BCrypt.Net.BCrypt.HashPassword("hunter2"));
        await cmd.ExecuteNonQueryAsync();

        // Generate RSA key pair for the test instance
        using var rsa = RSA.Create(2048);
        _privPath = Path.GetTempFileName();
        _pubPath  = Path.GetTempFileName();
        await File.WriteAllTextAsync(_privPath, rsa.ExportPkcs8PrivateKeyPem());
        await File.WriteAllTextAsync(_pubPath,  rsa.ExportRSAPublicKeyPem());
    }

    [Fact(Skip = "Docker not available in this environment — run manually when Docker Desktop is running")]
    public async Task LoginFlow_ValidCredentials_IssuesTokenAndRefreshes()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Identity:Issuer"]            = "https://identity.aqua/test",
                    ["Identity:Audience"]          = "aqua-api",
                    ["Identity:RsaPrivateKeyPath"] = _privPath,
                    ["Identity:RsaPublicKeyPath"]  = _pubPath,
                    ["AquaData:Tenants:acme:Dbms"]               = "Postgres",
                    ["AquaData:Tenants:acme:ConnectionString"]   = _pg.GetConnectionString(),
                });
            });
        });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Aqua-Tenant", "acme");

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/token", new { userName = "alice", password = "hunter2" });
        loginResp.IsSuccessStatusCode.Should().BeTrue();
        var loginBody = await loginResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        loginBody!["access_token"].ToString().Should().NotBeNullOrEmpty();

        // Refresh
        var refreshResp = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = loginBody["refresh_token"]!.ToString() });
        refreshResp.IsSuccessStatusCode.Should().BeTrue();

        // Reuse the OLD refresh token → must fail (reuse-detection)
        var reuseResp = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = loginBody["refresh_token"]!.ToString() });
        reuseResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    public async Task DisposeAsync()
    {
        if (File.Exists(_privPath)) File.Delete(_privPath);
        if (File.Exists(_pubPath))  File.Delete(_pubPath);
        await _pg.DisposeAsync();
    }
}
