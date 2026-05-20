using Aqua.UserService.Infrastructure;
using Aqua.UserService.Roles;
using Aqua.UserService.Roles.Dto;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Roles;

/// <summary>
/// Unit tests for <see cref="RoleController"/> — verifies the controller routes through to
/// <see cref="IRoleManager"/> and wraps Create/Patch responses in
/// <see cref="RoleCreateResponse"/> so warnings reach the client.
/// </summary>
public sealed class RoleControllerTests
{
    private readonly IRoleManager _mgr = Substitute.For<IRoleManager>();
    private readonly ITenantResolver _tenants = Substitute.For<ITenantResolver>();
    private readonly RoleController _sut;

    public RoleControllerTests()
    {
        _tenants.GetCurrentTenantIdAsync().Returns(Task.FromResult(7L));
        _sut = new RoleController(_mgr, _tenants);
    }

    [Fact]
    public async Task List_returns_roles_from_manager_for_current_tenant()
    {
        var roles = new List<RoleDto>();
        _mgr.ListAsync(7L).Returns(Task.FromResult<IReadOnlyList<RoleDto>>(roles));

        var result = await _sut.List();

        result.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(roles);
    }

    [Fact]
    public async Task Create_wraps_dto_and_warnings_in_response_envelope()
    {
        var req = new CreateRoleRequest("Admin", "All powers", Permissions: 0L,
            AvailableInProject: true, AvailableInCustomer: true, IsDefault: false);
        var dto = new RoleDto(99L, "Admin", "All powers", 7L, true, true, false,
            Permissions: 0L, PermVersion: "v1", Version: 0L);
        var warnings = new RoleMutationWarnings(new List<string> { "ReadUser" });
        _mgr.CreateAsync(req, 7L).Returns(Task.FromResult((dto, warnings)));

        var result = await _sut.Create(req);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(RoleController.Get));
        created.RouteValues!["id"].Should().Be(99L);
        var body = created.Value.Should().BeOfType<RoleCreateResponse>().Subject;
        body.Role.Should().Be(dto);
        body.Warnings.Should().Be(warnings);
    }

    [Fact]
    public async Task Delete_returns_NoContent_and_calls_manager()
    {
        var result = await _sut.Delete(42L);

        result.Should().BeOfType<NoContentResult>();
        await _mgr.Received(1).DeleteAsync(42L, 7L);
    }
}
