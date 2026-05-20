using Aqua.UserService.Events;
using Aqua.UserService.Infrastructure;
using Aqua.UserService.Tenants;
using Aqua.UserService.Tenants.Dto;
using Aqua.UserService.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NHibernate.Linq;
using Xunit;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Tests.Integration;

/// <summary>
/// End-to-end check that <see cref="TenantBootstrapper"/> emits the documented
/// <see cref="UserCreated"/> + <see cref="TenantCreated"/> outbox rows alongside the domain
/// inserts.
///
/// We don't drive this through HTTP because the request pipeline doesn't wrap a transaction
/// around the call yet (a per-request unit-of-work is on the roadmap but not in scope here).
/// Instead we resolve the bootstrapper from the same DI container the app uses, then flush
/// + commit explicitly so we can read the outbox table back via NHibernate.
/// </summary>
public sealed class EventFlowTests : IClassFixture<UserServiceWebApplicationFactory>
{
    private readonly UserServiceWebApplicationFactory _factory;
    public EventFlowTests(UserServiceWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task Bootstrap_writes_user_created_and_tenant_created_to_outbox()
    {
        // Slug must be DB-unique across the run; use a fresh GUID and keep within the 64 char limit.
        var slug = ("evt-" + Guid.NewGuid().ToString("N"))[..16];

        // Drive the call from the app's own service container so the publisher/outbox-writer
        // wire-up matches production. Flush + commit explicitly because UserService doesn't yet
        // wrap requests in a transactional unit-of-work.
        using (var scope = _factory.Services.CreateScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<ISession>();
            var bootstrapper = scope.ServiceProvider.GetRequiredService<ITenantBootstrapper>();
            using var tx = session.BeginTransaction();

            await bootstrapper.BootstrapAsync(new BootstrapTenantRequest(
                Slug: slug,
                DisplayName: "Evt Co",
                PrimaryDomain: null,
                Auth: new BootstrapTenantAuth(TenantAuthMode.Local, null, null),
                AdminUser: new BootstrapTenantAdmin(
                    Username: $"evt-admin-{slug}",
                    Email: $"{slug}@example.com",
                    FirstName: "I",
                    Surname: "A",
                    PasswordMode: "generate",
                    Password: null),
                DefaultRoles: "minimal"));

            await session.FlushAsync();
            await tx.CommitAsync();
        }

        // Read the outbox via the same SessionFactory the app uses. message_type is the
        // CLR full name of OutboxIntegrationEvent<UserCreated> / OutboxIntegrationEvent<TenantCreated>,
        // so a LIKE substring match on the inner event type is the most robust assertion.
        using var verify = _factory.Postgres.SessionFactory.OpenSession();
        var userRows = await verify.CreateSQLQuery(
                "SELECT message_type FROM messaging_outbox WHERE message_type LIKE :pat")
            .SetParameter("pat", "%UserCreated%")
            .ListAsync<string>();
        userRows.Should().NotBeEmpty()
            .And.Contain(r => r.Contains("UserCreated"));

        var tenantRows = await verify.CreateSQLQuery(
                "SELECT message_type FROM messaging_outbox WHERE message_type LIKE :pat")
            .SetParameter("pat", "%TenantCreated%")
            .ListAsync<string>();
        tenantRows.Should().NotBeEmpty()
            .And.Contain(r => r.Contains("TenantCreated"));
    }
}
