using Aqua.UserService.Infrastructure;
using Aqua.UserService.Infrastructure.Authorization;
using Aqua.UserService.Roles.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Roles;

/// <summary>
/// Tenant-scoped role management. Roles carry the permission bitset that authorizes
/// every other endpoint, so write access is gated on ManageRoles and read access on
/// ReadRole. The Create / Patch responses include the dependency-closure warnings
/// produced by <see cref="PermissionBitset.EnforceDependencies"/> — the UI surfaces
/// these so admins notice when implied bits were auto-added (e.g. ManageUsers also
/// grants ReadUser).
/// </summary>
[ApiController]
[Route("api/v1/roles")]
public sealed class RoleController : ControllerBase
{
    private readonly IRoleManager _mgr;
    private readonly ITenantResolver _tenants;

    public RoleController(IRoleManager mgr, ITenantResolver tenants)
    {
        _mgr = mgr;
        _tenants = tenants;
    }

    [HttpGet]
    [Permission(Permission.ReadRole)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List()
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        return Ok(await _mgr.ListAsync(customerId));
    }

    [HttpGet("{id:long}")]
    [Permission(Permission.ReadRole)]
    public async Task<ActionResult<RoleDto>> Get(long id) =>
        Ok(await _mgr.GetAsync(id));

    [HttpPost]
    [Permission(Permission.ManageRoles)]
    public async Task<ActionResult<RoleCreateResponse>> Create(CreateRoleRequest req)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        var (dto, warnings) = await _mgr.CreateAsync(req, customerId);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, new RoleCreateResponse(dto, warnings));
    }

    [HttpPatch("{id:long}")]
    [Permission(Permission.ManageRoles)]
    public async Task<ActionResult<RoleCreateResponse>> Patch(long id, PatchRoleRequest req)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        var (dto, warnings) = await _mgr.PatchAsync(id, req, customerId);
        return Ok(new RoleCreateResponse(dto, warnings));
    }

    [HttpDelete("{id:long}")]
    [Permission(Permission.ManageRoles)]
    public async Task<IActionResult> Delete(long id)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        await _mgr.DeleteAsync(id, customerId);
        return NoContent();
    }
}

/// <summary>
/// Shared response envelope for role mutations — wraps the persisted role plus the
/// permission dependency-closure warnings so clients can surface both in one round-trip.
/// </summary>
public sealed record RoleCreateResponse(RoleDto Role, RoleMutationWarnings Warnings);
