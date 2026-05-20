using Aqua.UserService.Domain;
using Aqua.UserService.Events;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants;
using Aqua.UserService.Tenants.Dto;
using Aqua.UserService.Tests.TestSupport;
using Aqua.UserService.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Tenants;

public sealed class TenantBootstrapperTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly ICustomerUserAssignmentRepository _cuas = Substitute.For<ICustomerUserAssignmentRepository>();
    private readonly IUserEventPublisher _publisher = Substitute.For<IUserEventPublisher>();
    private readonly TenantBootstrapper _sut;

    public TenantBootstrapperTests()
    {
        _sut = new TenantBootstrapper(_customers, _users, _roles, _cuas, _publisher);
    }

    private static BootstrapTenantRequest StandardRequest(
        string slug = "acme",
        string username = "admin",
        string email = "admin@acme.com",
        string defaultRoles = "standard",
        string passwordMode = "generate",
        string? password = null) =>
        new(
            Slug: slug,
            DisplayName: "Acme Inc.",
            PrimaryDomain: "acme.com",
            Auth: new BootstrapTenantAuth(TenantAuthMode.Local, null, null),
            AdminUser: new BootstrapTenantAdmin(
                Username: username,
                Email: email,
                FirstName: "Admin",
                Surname: "User",
                PasswordMode: passwordMode,
                Password: password),
            DefaultRoles: defaultRoles);

    [Fact]
    public async Task Bootstrap_creates_customer_admin_and_default_roles()
    {
        _customers.FindBySlugAsync("acme").Returns(Task.FromResult<Customer?>(null));
        // Assign IDs as inserts happen (simulating NHibernate identity assignment).
        _customers.InsertAsync(Arg.Do<Customer>(c => c.Id = 42L)).Returns(Task.CompletedTask);
        _roles.InsertAsync(Arg.Do<Role>(r => r.Id = Random.Shared.NextInt64(1000, 99999)))
            .Returns(Task.CompletedTask);
        _users.InsertAsync(Arg.Do<User>(u => u.Id = 7L)).Returns(Task.CompletedTask);

        var resp = await _sut.BootstrapAsync(StandardRequest());

        resp.TenantId.Should().Be(42L);
        resp.TenantSlug.Should().Be("acme");
        resp.AdminUserId.Should().Be(7L);
        resp.AdminUsername.Should().Be("admin");
        resp.Skipped.Should().BeFalse();
        resp.RolesCreated.Should().Be(5);
        resp.InitialPassword.Should().NotBeNullOrEmpty();

        await _customers.Received(1).InsertAsync(Arg.Any<Customer>());
        await _roles.Received(5).InsertAsync(Arg.Any<Role>());
        await _users.Received(1).InsertAsync(Arg.Is<User>(u => u.ServerAdmin && u.CustomerIdHint == 42L));
        await _publisher.Received(1).PublishAsync(42L, "tenant.created",
            Arg.Any<TenantCreated>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(42L, "user.created",
            Arg.Is<UserCreated>(e => e.IsFirstAdmin && !e.IsLdap),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Bootstrap_is_idempotent_for_same_slug()
    {
        var existing = new TenantBuilder().WithSlug("acme").Build();
        existing.Id = 42L;
        var existingAdmin = new UserBuilder().WithUsername("admin").WithEmail("admin@acme.com").Build();
        existingAdmin.Id = 7L;
        _customers.FindBySlugAsync("acme").Returns(Task.FromResult<Customer?>(existing));
        _users.FindByUsernameAsync("admin").Returns(Task.FromResult<User?>(existingAdmin));

        var resp = await _sut.BootstrapAsync(StandardRequest());

        resp.Skipped.Should().BeTrue();
        resp.TenantId.Should().Be(42L);
        resp.AdminUserId.Should().Be(7L);
        resp.RolesCreated.Should().Be(0);
        resp.InitialPassword.Should().BeNull();

        await _customers.DidNotReceive().InsertAsync(Arg.Any<Customer>());
        await _users.DidNotReceive().InsertAsync(Arg.Any<User>());
        await _roles.DidNotReceive().InsertAsync(Arg.Any<Role>());
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<long>(), Arg.Any<string>(),
            Arg.Any<TenantCreated>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Bootstrap_conflicting_admin_username_throws()
    {
        var existing = new TenantBuilder().WithSlug("acme").Build();
        existing.Id = 42L;
        var existingAdmin = new UserBuilder().WithUsername("admin")
            .WithEmail("different@acme.com").Build();
        existingAdmin.Id = 7L;
        _customers.FindBySlugAsync("acme").Returns(Task.FromResult<Customer?>(existing));
        _users.FindByUsernameAsync("admin").Returns(Task.FromResult<User?>(existingAdmin));

        var act = () => _sut.BootstrapAsync(StandardRequest(email: "admin@acme.com"));

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.ErrorCode == "tenant.bootstrap-conflict");
    }
}
