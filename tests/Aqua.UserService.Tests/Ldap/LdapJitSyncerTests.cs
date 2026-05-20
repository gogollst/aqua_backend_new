using Aqua.UserService.Domain;
using Aqua.UserService.Ldap;
using Aqua.UserService.Ldap.Dto;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants;
using Aqua.UserService.Users;
using Aqua.UserService.Tests.TestSupport;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Ldap;

public sealed class LdapJitSyncerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ILdapGroupRoleMappingRepository _mappings = Substitute.For<ILdapGroupRoleMappingRepository>();
    private readonly ICustomerUserAssignmentRepository _cuas = Substitute.For<ICustomerUserAssignmentRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly Aqua.UserService.Events.IUserEventPublisher _publisher =
        Substitute.For<Aqua.UserService.Events.IUserEventPublisher>();
    private readonly LdapJitSyncer _sut;

    public LdapJitSyncerTests()
    {
        _sut = new LdapJitSyncer(_customers, _users, _mappings, _cuas, _roles, _publisher);
    }

    [Fact]
    public async Task First_login_creates_user_and_assigns_mapped_roles()
    {
        var customer = new TenantBuilder().WithSlug("acme").WithAuthMode(TenantAuthMode.Ldap).Build();
        customer.Id = 1L;
        _customers.FindBySlugAsync("acme").Returns(Task.FromResult<Customer?>(customer));
        _users.FindByLdapDnAsync(1L, "uid=alice,dc=acme").Returns(Task.FromResult<User?>(null));
        _users.FindByUsernameAsync("alice").Returns(Task.FromResult<User?>(null));
        _mappings.ListAsync(1L).Returns(Task.FromResult<IReadOnlyList<LdapGroupRoleMapping>>(new[]
        {
            new LdapGroupRoleMapping { CustomerId = 1L, LdapGroupDn = "cn=qa-leads,dc=acme", RoleId = 100L },
        }));
        _cuas.GetRoleIdsAsync(1L, Arg.Any<long>())
            .Returns(Task.FromResult<IReadOnlyList<long>>(Array.Empty<long>()));
        _roles.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>())
            .Returns(Task.FromResult<IReadOnlyList<Role>>(new[]
            {
                new RoleBuilder().WithName("QA-Lead").WithPerms(Permission.WriteTestCase).Build()
            }));

        var resp = await _sut.SyncAsync(new LdapJitSyncRequest(
            CustomerSlug: "acme",
            LdapDn: "uid=alice,dc=acme",
            Username: "alice",
            Email: "alice@acme.com",
            FirstName: "Alice",
            Surname: "Anderson",
            Groups: new[] { "cn=qa-leads,dc=acme" }));

        resp.IsNewUser.Should().BeTrue();
        resp.Roles.Should().Contain("QA-Lead");
        await _users.Received(1).InsertAsync(Arg.Any<User>());
        await _publisher.Received(1).PublishAsync(1L, "user.created",
            Arg.Any<Aqua.UserService.Events.UserCreated>(), Arg.Any<CancellationToken>());
        await _cuas.Received(1).AssignRolesAsync(1L, Arg.Any<long>(),
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.Contains(100L)));
    }

    [Fact]
    public async Task Zero_mapped_groups_throws_forbidden()
    {
        var customer = new TenantBuilder().WithSlug("acme").Build();
        customer.Id = 1L;
        _customers.FindBySlugAsync("acme").Returns(Task.FromResult<Customer?>(customer));
        _users.FindByLdapDnAsync(1L, "uid=alice,dc=acme").Returns(Task.FromResult<User?>(null));
        _users.FindByUsernameAsync("alice").Returns(Task.FromResult<User?>(null));
        _mappings.ListAsync(1L)
            .Returns(Task.FromResult<IReadOnlyList<LdapGroupRoleMapping>>(Array.Empty<LdapGroupRoleMapping>()));

        var act = () => _sut.SyncAsync(new LdapJitSyncRequest(
            CustomerSlug: "acme",
            LdapDn: "uid=alice,dc=acme",
            Username: "alice",
            Email: "alice@acme.com",
            FirstName: "Alice",
            Surname: "Anderson",
            Groups: new[] { "cn=unmapped,dc=acme" }));

        await act.Should().ThrowAsync<ForbiddenException>()
            .Where(e => e.ErrorCode == "ldap-jit.no-roles");
    }

    [Fact]
    public async Task Subsequent_login_diffs_roles_and_emits_role_changed_event()
    {
        var customer = new TenantBuilder().Build();
        customer.Id = 1L;
        var existing = new UserBuilder().WithUsername("alice").WithLdapDn("uid=alice,dc=acme").Build();
        existing.Id = 17L;
        _customers.FindBySlugAsync("acme").Returns(Task.FromResult<Customer?>(customer));
        _users.FindByLdapDnAsync(1L, "uid=alice,dc=acme").Returns(Task.FromResult<User?>(existing));
        _cuas.GetRoleIdsAsync(1L, 17L).Returns(Task.FromResult<IReadOnlyList<long>>(new[] { 100L }));
        _mappings.ListAsync(1L).Returns(Task.FromResult<IReadOnlyList<LdapGroupRoleMapping>>(new[]
        {
            new LdapGroupRoleMapping { LdapGroupDn = "cn=devs,dc=acme", RoleId = 200L },
        }));
        _roles.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>())
            .Returns(Task.FromResult<IReadOnlyList<Role>>(new[]
            {
                new RoleBuilder().WithName("Developer").Build()
            }));

        var resp = await _sut.SyncAsync(new LdapJitSyncRequest(
            "acme", "uid=alice,dc=acme", "alice", "alice@acme.com",
            "Alice", "Anderson", new[] { "cn=devs,dc=acme" }));

        resp.IsNewUser.Should().BeFalse();
        await _users.DidNotReceive().InsertAsync(Arg.Any<User>());
        await _publisher.Received(1).PublishAsync(1L, "user.role-changed",
            Arg.Is<Aqua.UserService.Events.UserRoleChanged>(e =>
                e.OldRoleIds.SequenceEqual(new[] { 100L }) &&
                e.NewRoleIds.SequenceEqual(new[] { 200L }) &&
                e.Source == "LdapSync"),
            Arg.Any<CancellationToken>());
    }
}
