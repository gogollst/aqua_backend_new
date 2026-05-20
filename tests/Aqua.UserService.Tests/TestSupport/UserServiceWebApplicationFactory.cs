using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NHibernate;
using Xunit;

namespace Aqua.UserService.Tests.TestSupport;

/// <summary>
/// WebApplicationFactory that hosts the UserService in-memory on a <see cref="TestServer"/> and
/// wires it to a fresh PostgreSQL test container. Configures JWT-bearer auth to validate
/// tokens issued by <see cref="TestJwtBuilder"/>, and replaces the <see cref="ISessionFactory"/>
/// registration with the fixture-built one so tests and the app share the same migrated schema.
/// </summary>
public sealed class UserServiceWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgresFixture? _postgres;

    public PostgresFixture Postgres => _postgres ?? throw new InvalidOperationException("Fixture not initialized.");

    public async Task InitializeAsync()
    {
        _postgres = new PostgresFixture();
        await _postgres.InitializeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_postgres is not null) await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("UserService:ConnectionString", _postgres!.ConnectionString);
        // PublicPort/InternalPort drive Kestrel in Program.cs, but UseTestServer below replaces
        // Kestrel entirely. Setting them to 0 is harmless and keeps Program.cs config-reads happy.
        builder.UseSetting("UserService:PublicPort",   "0");
        builder.UseSetting("UserService:InternalPort", "0");
        builder.UseSetting("InternalApi:Token", "test-internal-token");
        builder.UseSetting("InternalApi:RequireMtls", "false");
        builder.UseSetting("IdentityService:Authority", "https://test.aqua-cloud.io");

        // TestServer replaces Kestrel — no real socket binding, the port-filter middleware in
        // Program.cs uses ctx.Connection.LocalPort which TestServer sets to 0 on both branches.
        builder.UseTestServer();

        builder.ConfigureTestServices(services =>
        {
            // Override JwtBearer to validate test-issued JWTs (HS256 with test key) without
            // reaching out to a real authority's OIDC discovery endpoint.
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                opts.Authority = null;
                opts.RequireHttpsMetadata = false;
                opts.ConfigurationManager = null;
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestJwtBuilder.Issuer,
                    ValidateAudience = true,
                    ValidAudience = TestJwtBuilder.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(TestJwtBuilder.SigningKey)),
                    ValidateLifetime = true,
                };
            });

            // Replace ISessionFactory with the test-fixture-built one (shares migrated schema
            // and avoids spinning up a second SessionFactory against the same DB).
            var sfDescriptors = services.Where(d => d.ServiceType == typeof(ISessionFactory)).ToList();
            foreach (var d in sfDescriptors) services.Remove(d);
            services.AddSingleton<ISessionFactory>(_ => _postgres!.SessionFactory);
        });
    }

    /// <summary>
    /// Issues a bearer token for the given user/tenant and attaches both the
    /// <c>Authorization</c> header and the tenant context header (<c>X-Aqua-Tenant</c>).
    /// </summary>
    public HttpClient CreateAuthenticated(long userId, string tenantSlug, long tenantId = 1L,
        long perms = 0, bool serverAdmin = false)
    {
        var builder = new TestJwtBuilder()
            .ForUser(userId)
            .InTenant(tenantSlug, tenantId)
            .WithPerms(perms);
        if (serverAdmin) builder.AsServerAdmin();
        var token = builder.Build();

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Aqua-Tenant", tenantSlug);
        return client;
    }

    public HttpClient CreateAnonymous(string tenantSlug)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Aqua-Tenant", tenantSlug);
        return client;
    }

    public HttpClient CreateInternal(string token = "test-internal-token")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Token", token);
        return client;
    }

    /// <summary>
    /// Seeds the test database via NHibernate. Returns the last-saved user and the tenant that
    /// hosts it, so the caller can build an authenticated client for that user.
    /// </summary>
    public async Task<(long UserId, string TenantSlug, long TenantId)> SeedAsync(Action<TestSeed> configure)
    {
        var seed = new TestSeed(_postgres!.SessionFactory);
        configure(seed);
        await seed.PersistAsync();
        return (seed.LastUserId, seed.LastTenantSlug, seed.LastTenantId);
    }
}
