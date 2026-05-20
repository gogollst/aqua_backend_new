using Aqua.UserService.Infrastructure;
using Aqua.UserService.Infrastructure.Authorization;
using Aqua.UserService.Ldap.Dto;
using Aqua.UserService.Roles;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Ldap;

/// <summary>
/// Tenant admin surface for LDAP-group -> role mappings. Reuses the
/// <see cref="Permission.ManageRoles"/> bit because mappings effectively grant role
/// membership at JIT-sync time.
/// </summary>
[ApiController]
[Route("api/v1/ldap/group-mappings")]
public sealed class LdapGroupMappingController : ControllerBase
{
    private readonly ILdapGroupMappingManager _mgr;
    private readonly ITenantResolver _tenants;

    public LdapGroupMappingController(ILdapGroupMappingManager mgr, ITenantResolver tenants)
    {
        _mgr = mgr;
        _tenants = tenants;
    }

    [HttpGet]
    [Permission(Permission.ManageRoles)]
    public async Task<ActionResult<IReadOnlyList<LdapGroupMappingDto>>> List() =>
        Ok(await _mgr.ListAsync(await _tenants.GetCurrentTenantIdAsync()));

    [HttpPost]
    [Permission(Permission.ManageRoles)]
    public async Task<ActionResult<LdapGroupMappingDto>> Create(CreateLdapMappingRequest req)
    {
        var dto = await _mgr.CreateAsync(req, await _tenants.GetCurrentTenantIdAsync());
        return Created($"/api/v1/ldap/group-mappings/{dto.Id}", dto);
    }

    [HttpDelete("{id:long}")]
    [Permission(Permission.ManageRoles)]
    public async Task<IActionResult> Delete(long id)
    {
        await _mgr.DeleteAsync(id, await _tenants.GetCurrentTenantIdAsync());
        return NoContent();
    }
}
