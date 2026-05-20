using Aqua.UserService.Persistence;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants;
using Aqua.UserService.Tests.TestSupport;
using Aqua.UserService.Users;
using FluentAssertions;
using NHibernate.Linq;
using Xunit;

namespace Aqua.UserService.Tests.Integration;

/// <summary>
/// End-to-end checks that the NHibernate <c>tenant_filter</c> hides rows belonging to other
/// customers. These tests bypass the HTTP pipeline and drive the filter directly on the
/// fixture's <see cref="NHibernate.ISession"/>, which is what the request-scoped session
/// registered in <c>Program.cs</c> does after <c>TenantContextMiddleware</c> resolves a tenant.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TenantIsolationTests : TransactionalTest
{
    public TenantIsolationTests(PostgresFixture fx) : base(fx) { }

    [Fact]
    public async Task User_queries_filtered_by_tenant_id()
    {
        var custA = new Customer { Slug = "ti-a", DisplayName = "A" };
        await Session.SaveAsync(custA);
        var userA = new User { Username = "ti-alice", CustomerIdHint = custA.Id, Status = UserStatus.Active };
        await Session.SaveAsync(userA);

        var custB = new Customer { Slug = "ti-b", DisplayName = "B" };
        await Session.SaveAsync(custB);
        var userB = new User { Username = "ti-bob", CustomerIdHint = custB.Id, Status = UserStatus.Active };
        await Session.SaveAsync(userB);
        await Session.FlushAsync();
        Session.Clear();

        TenantFilter.EnableFor(Session, custA.Id);
        var visible = await Session.Query<User>().Where(u => u.Username.StartsWith("ti-")).ToListAsync();
        visible.Should().ContainSingle().Which.Username.Should().Be("ti-alice");
    }

    [Fact]
    public async Task Roles_queries_filtered_by_tenant_id()
    {
        var custA = new Customer { Slug = "ti-ra", DisplayName = "A" };
        await Session.SaveAsync(custA);
        await Session.SaveAsync(new Role { Name = "ti-Admin", CustomerId = custA.Id, Permissions = PermissionBitset.None });
        var custB = new Customer { Slug = "ti-rb", DisplayName = "B" };
        await Session.SaveAsync(custB);
        await Session.SaveAsync(new Role { Name = "ti-Reader", CustomerId = custB.Id, Permissions = PermissionBitset.None });
        await Session.FlushAsync();
        Session.Clear();

        TenantFilter.EnableFor(Session, custB.Id);
        var roles = await Session.Query<Role>().Where(r => r.Name.StartsWith("ti-")).ToListAsync();
        roles.Should().ContainSingle().Which.Name.Should().Be("ti-Reader");
    }

    [Fact]
    public async Task CustomerUserAssignment_queries_filtered_by_tenant_id()
    {
        var custA = new Customer { Slug = "ti-ca", DisplayName = "A" };
        await Session.SaveAsync(custA);
        await Session.SaveAsync(new CustomerUserAssignment { CustomerId = custA.Id, UserId = 1, RoleId = 1 });

        var custB = new Customer { Slug = "ti-cb", DisplayName = "B" };
        await Session.SaveAsync(custB);
        await Session.SaveAsync(new CustomerUserAssignment { CustomerId = custB.Id, UserId = 2, RoleId = 2 });
        await Session.FlushAsync();
        Session.Clear();

        TenantFilter.EnableFor(Session, custA.Id);
        var visible = await Session.Query<CustomerUserAssignment>()
            .Where(a => a.CustomerId == custA.Id || a.CustomerId == custB.Id)
            .ToListAsync();
        visible.Should().ContainSingle().Which.UserId.Should().Be(1);
    }
}
