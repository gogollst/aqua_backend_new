using Aqua.UserService.ClaimsLookup;
using Aqua.UserService.Domain;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants;
using Aqua.UserService.Tests.TestSupport;
using Aqua.UserService.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.ClaimsLookup;

public sealed class ClaimsLookupServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerUserAssignmentRepository _cuas = Substitute.For<ICustomerUserAssignmentRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly ClaimsLookupService _sut;

    public ClaimsLookupServiceTests()
    {
        _sut = new ClaimsLookupService(_users, _customers, _cuas, _roles);
    }

    [Fact]
    public async Task Returns_aggregated_claims_for_user_in_tenant()
    {
        var user = new UserBuilder().WithUsername("alice").Build();
        user.Id = 17L;
        user.Email = "alice@x.com";
        user.Status = UserStatus.Active;

        var customer = new TenantBuilder().WithSlug("acme").Build();
        customer.Id = 1L;

        var adminRole = new RoleBuilder().WithName("Admin")
            .WithPerms(Permission.ManageUsers | Permission.ReadUser).Build();
        adminRole.Id = 100L;

        _customers.FindBySlugAsync("acme").Returns(Task.FromResult<Customer?>(customer));
        _users.FindByIdAsync(17L).Returns(Task.FromResult<User?>(user));
        _cuas.GetRoleIdsAsync(1L, 17L).Returns(Task.FromResult<IReadOnlyList<long>>(new[] { 100L }));
        _roles.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>())
            .Returns(Task.FromResult<IReadOnlyList<Role>>(new[] { adminRole }));

        var resp = await _sut.LookupAsync(userId: 17L, tenantSlug: "acme");

        resp.Sub.Should().Be("17");
        resp.Email.Should().Be("alice@x.com");
        resp.IsActive.Should().BeTrue();
        resp.Roles.Should().BeEquivalentTo(new[] { "Admin" });
        resp.PermsBitset.Should().Be((long)(Permission.ManageUsers | Permission.ReadUser));
    }

    [Fact]
    public async Task User_not_found_throws()
    {
        var customer = new TenantBuilder().WithSlug("acme").Build();
        customer.Id = 1L;
        _customers.FindBySlugAsync("acme").Returns(Task.FromResult<Customer?>(customer));
        _users.FindByIdAsync(17L).Returns(Task.FromResult<User?>(null));

        var act = () => _sut.LookupAsync(userId: 17L, tenantSlug: "acme");
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
