using Aqua.UserService.Roles.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Roles;

/// <summary>
/// Static permission catalog — the canonical list of permission bits with their
/// human-readable labels (en/de) and dependency closures. The UI uses it to render
/// role-edit forms. The payload is identical for every tenant and rarely changes,
/// so it's marked as cacheable for one hour to keep the request rate low.
/// </summary>
[ApiController]
[Route("api/v1/permissions")]
[Authorize]
public sealed class PermissionCatalogController : ControllerBase
{
    [HttpGet("catalog")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public ActionResult<PermissionCatalogDto> Get() => Ok(PermissionCatalog.Build());
}
