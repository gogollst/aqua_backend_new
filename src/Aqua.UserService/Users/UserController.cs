using Aqua.UserService.Events;
using Aqua.UserService.Infrastructure;
using Aqua.UserService.Infrastructure.Authorization;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants;
using Aqua.UserService.Users.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Users;

/// <summary>
/// Tenant-scoped user administration. All mutating endpoints require ManageUsers; read
/// endpoints require ReadUser. The /lookup endpoint only requires authentication because
/// the UI calls it from many surfaces (assignee pickers, watcher lists) where the caller
/// already legitimately holds user ids — gating it on ReadUser would break the picker
/// for non-admin authors.
/// </summary>
[ApiController]
[Route("api/v1/users")]
public sealed class UserController : ControllerBase
{
    private readonly IUserManager _users;
    private readonly ICustomerUserAssignmentRepository _cuas;
    private readonly IUserEventPublisher _publisher;
    private readonly ITenantResolver _tenants;

    public UserController(
        IUserManager users,
        ICustomerUserAssignmentRepository cuas,
        IUserEventPublisher publisher,
        ITenantResolver tenants)
    {
        _users = users;
        _cuas = cuas;
        _publisher = publisher;
        _tenants = tenants;
    }

    [HttpGet]
    [Permission(Permission.ReadUser)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? search = null)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        return Ok(await _users.ListAsync(customerId, skip, take, search));
    }

    [HttpGet("{id:long}")]
    [Permission(Permission.ReadUser)]
    public async Task<ActionResult<UserDto>> Get(long id) =>
        Ok(await _users.GetAsync(id));

    [HttpPost]
    [Permission(Permission.ManageUsers)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest req)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        var dto = await _users.CreateAsync(req, customerId);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPatch("{id:long}")]
    [Permission(Permission.ManageUsers)]
    public async Task<ActionResult<UserDto>> Patch(long id, PatchUserRequest req)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        return Ok(await _users.PatchAsync(id, req, customerId));
    }

    [HttpDelete("{id:long}")]
    [Permission(Permission.ManageUsers)]
    public async Task<IActionResult> Delete(long id)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        await _users.SoftDeleteAsync(id, customerId);
        return NoContent();
    }

    [HttpGet("{id:long}/roles")]
    [Permission(Permission.ReadUser)]
    public async Task<ActionResult<IReadOnlyList<long>>> GetRoles(long id)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        return Ok(await _cuas.GetRoleIdsAsync(customerId, id));
    }

    [HttpPut("{id:long}/roles")]
    [Permission(Permission.ManageUsers)]
    public async Task<IActionResult> AssignRoles(long id, AssignUserRolesRequest req)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        // Capture the previous role set BEFORE the write so the role-changed event
        // carries an accurate old → new diff. Both lists are sorted to make event
        // payloads deterministic for downstream consumers / contract tests.
        var current = await _cuas.GetRoleIdsAsync(customerId, id);
        await _cuas.AssignRolesAsync(customerId, id, req.RoleIds);
        await _publisher.PublishAsync(customerId, "user.role-changed",
            new UserRoleChanged(
                UserId: id,
                OldRoleIds: current.OrderBy(x => x).ToList(),
                NewRoleIds: req.RoleIds.OrderBy(x => x).ToList(),
                Source: "Admin"));
        return NoContent();
    }

    [HttpPost("lookup")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> Lookup(BulkUserLookupRequest req) =>
        Ok(await _users.GetByIdsAsync(req.Ids));
}
