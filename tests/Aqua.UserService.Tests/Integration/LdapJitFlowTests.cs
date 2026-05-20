using System.Net;
using System.Net.Http.Json;
using Aqua.UserService.Ldap;
using Aqua.UserService.Ldap.Dto;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants;
using Aqua.UserService.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Integration;

/// <summary>
/// HTTP-level integration tests for the LDAP just-in-time provisioning flow. Drives the request
/// through the live ASP.NET pipeline (TestServer), the <c>InternalApi</c> auth scheme, validation,
/// the <c>LdapJitSyncer</c>, NHibernate, and the exception filter — i.e. everything between the
/// identity-service caller and the outbox write.
///
/// We do not run an OpenLDAP testcontainer here: <c>LdapJitSyncer.SyncAsync</c> receives a
/// pre-parsed <see cref="LdapJitSyncRequest"/> (identity-service does the bind+group-resolve in
/// SS-05). A real LDAP container will be added in E2E Stage 13 when identity-service drives the
/// bind itself.
/// </summary>
public sealed class LdapJitFlowTests : IClassFixture<UserServiceWebApplicationFactory>
{
    private readonly UserServiceWebApplicationFactory _factory;
    public LdapJitFlowTests(UserServiceWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task JitSync_creates_new_user_when_first_login()
    {
        // Slug must stay <= 64 chars (CustomerMapping.hbm.xml) and unique across the run.
        var slug = ("ldap-t-" + Guid.NewGuid().ToString("N"))[..16];
        await SeedTenantRoleAndMapping(slug, "QA-Lead", "cn=qa-leads,dc=acme");

        var client = _factory.CreateInternal();
        var resp = await client.PostAsJsonAsync("/internal/v1/ldap/jit-sync",
            new LdapJitSyncRequest(
                CustomerSlug: slug,
                LdapDn: $"uid=alice-{slug},dc=acme",
                Username: $"alice-{slug}",
                // Use a syntactically valid email — FluentValidation .EmailAddress() rejects
                // bare-host forms like "alice@acme".
                Email: $"alice-{slug}@acme.test",
                FirstName: "Alice",
                Surname: "Anderson",
                Groups: new[] { "cn=qa-leads,dc=acme" }));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<LdapJitSyncResponse>();
        body.Should().NotBeNull();
        body!.IsNewUser.Should().BeTrue();
        body.Roles.Should().Contain("QA-Lead");
    }

    [Fact]
    public async Task JitSync_with_no_mapped_groups_returns_403()
    {
        var slug = ("ldap-t-" + Guid.NewGuid().ToString("N"))[..16];
        await SeedTenantRoleAndMapping(slug, "Dev", "cn=devs,dc=acme");

        var client = _factory.CreateInternal();
        var resp = await client.PostAsJsonAsync("/internal/v1/ldap/jit-sync",
            new LdapJitSyncRequest(
                CustomerSlug: slug,
                LdapDn: $"uid=bob-{slug},dc=acme",
                Username: $"bob-{slug}",
                Email: $"bob-{slug}@acme.test",
                FirstName: "Bob",
                Surname: "Bot",
                Groups: new[] { "cn=unmapped,dc=acme" }));

        // LdapJitSyncer throws ForbiddenException("ldap-jit.no-roles"), mapped by
        // ExceptionMappingFilter to 403 application/problem+json.
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task SeedTenantRoleAndMapping(string slug, string roleName, string ldapGroupDn)
    {
        using var session = _factory.Postgres.SessionFactory.OpenSession();
        using var tx = session.BeginTransaction();

        var customer = new Customer
        {
            Slug = slug,
            DisplayName = "L",
            AuthMode = TenantAuthMode.Ldap,
        };
        await session.SaveAsync(customer);

        var role = new Role
        {
            Name = roleName,
            CustomerId = customer.Id,
            Permissions = PermissionBitset.From(Permission.ReadRequirement),
        };
        await session.SaveAsync(role);

        await session.SaveAsync(new LdapGroupRoleMapping
        {
            CustomerId = customer.Id,
            LdapGroupDn = ldapGroupDn,
            RoleId = role.Id,
            CreatedAt = DateTime.UtcNow,
        });

        await tx.CommitAsync();
    }
}
