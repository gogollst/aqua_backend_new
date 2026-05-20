using Aqua.UserService.Domain;
using Aqua.UserService.Roles;
using Aqua.UserService.Roles.Dto;
using Aqua.UserService.Tests.TestSupport;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Roles;

public sealed class RoleManagerTests
{
    private readonly IRoleRepository _repo = Substitute.For<IRoleRepository>();
    private readonly RoleManager _sut;

    public RoleManagerTests() => _sut = new RoleManager(_repo);

    [Fact]
    public async Task Create_auto_adds_dependency_perms_and_lists_them_as_warnings()
    {
        var req = new CreateRoleRequest("Dev", "Developer role",
            (long)Permission.WriteRequirement,
            AvailableInProject: true, AvailableInCustomer: true, IsDefault: false);
        _repo.FindByNameAsync(1L, "Dev").Returns(Task.FromResult<Role?>(null));

        var (dto, warnings) = await _sut.CreateAsync(req, customerId: 1L);

        warnings.AdjustedPermissions.Should().Contain("ReadRequirement");
        (dto.Permissions & (long)Permission.ReadRequirement).Should().Be((long)Permission.ReadRequirement);
        (dto.Permissions & (long)Permission.WriteRequirement).Should().Be((long)Permission.WriteRequirement);
    }

    [Fact]
    public async Task Create_throws_on_duplicate_name_within_tenant()
    {
        _repo.FindByNameAsync(1L, "Dev").Returns(Task.FromResult<Role?>(new RoleBuilder().WithName("Dev").Build()));
        var req = new CreateRoleRequest("Dev", null, 0, true, true, false);

        var act = () => _sut.CreateAsync(req, customerId: 1L);

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.ErrorCode == "role.name-taken");
    }

    [Fact]
    public async Task Patch_with_stale_version_throws()
    {
        var existing = new RoleBuilder().Build();
        existing.Id = 17L;
        existing.Version = 5;
        _repo.FindByIdAsync(17L).Returns(Task.FromResult<Role?>(existing));

        var act = () => _sut.PatchAsync(17L, new PatchRoleRequest(null, null, null, null, null, null, Version: 4), customerId: 1L);

        await act.Should().ThrowAsync<StaleVersionException>();
    }

    [Fact]
    public async Task Delete_removes_role_and_returns_void()
    {
        var existing = new RoleBuilder().Build();
        existing.Id = 17L;
        _repo.FindByIdAsync(17L).Returns(Task.FromResult<Role?>(existing));

        await _sut.DeleteAsync(17L, customerId: 1L);

        await _repo.Received(1).DeleteAsync(existing);
    }

    [Fact]
    public async Task Patch_with_new_permissions_re_runs_dependency_closure()
    {
        var existing = new RoleBuilder().Build();
        existing.Id = 17L;
        existing.Version = 0;
        _repo.FindByIdAsync(17L).Returns(Task.FromResult<Role?>(existing));

        // ManageRoles implies both ReadRole and ReadUser (per PermissionDependencies map).
        var req = new PatchRoleRequest(
            Name: null,
            Description: null,
            Permissions: (long)Permission.ManageRoles,
            AvailableInProject: null,
            AvailableInCustomer: null,
            IsDefault: null,
            Version: 0);

        var (dto, warnings) = await _sut.PatchAsync(17L, req, customerId: 1L);

        warnings.AdjustedPermissions.Should().Contain("ReadRole");
        warnings.AdjustedPermissions.Should().Contain("ReadUser");
        (dto.Permissions & (long)Permission.ReadRole).Should().Be((long)Permission.ReadRole);
        (dto.Permissions & (long)Permission.ReadUser).Should().Be((long)Permission.ReadUser);
        (dto.Permissions & (long)Permission.ManageRoles).Should().Be((long)Permission.ManageRoles);
    }
}
