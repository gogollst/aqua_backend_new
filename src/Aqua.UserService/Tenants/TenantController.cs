using Aqua.UserService.Infrastructure.Authorization;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Tenants;

/// <summary>
/// Tenant settings administration. Reads and partial-updates are gated on the
/// tenant-specific permission bits so a tenant admin can only manage their own
/// settings; cross-tenant operations are not exposed here.
/// </summary>
[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantController : ControllerBase
{
    private readonly ITenantManager _mgr;

    public TenantController(ITenantManager mgr) => _mgr = mgr;

    [HttpGet("{id:long}/settings")]
    [Permission(Permission.ReadTenantSettings)]
    public async Task<ActionResult<TenantSettingsDto>> Get(long id) =>
        Ok(await _mgr.GetSettingsAsync(id));

    [HttpPatch("{id:long}/settings")]
    [Permission(Permission.ManageTenantSettings)]
    public async Task<ActionResult<TenantSettingsDto>> Patch(long id, PatchTenantSettingsRequest req) =>
        Ok(await _mgr.PatchSettingsAsync(id, req));
}
