using Aqua.UserService.Infrastructure.Authorization;
using Aqua.UserService.Tenants.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Tenants;

[ApiController]
[Route("internal/v1/tenants")]
[Authorize(AuthenticationSchemes = InternalApiAuthHandler.SchemeName)]
public sealed class InternalTenantController : ControllerBase
{
    private readonly ITenantBootstrapper _bootstrapper;
    public InternalTenantController(ITenantBootstrapper bootstrapper) => _bootstrapper = bootstrapper;

    [HttpPost("bootstrap")]
    public async Task<ActionResult<BootstrapTenantResponse>> Bootstrap(BootstrapTenantRequest req)
    {
        var resp = await _bootstrapper.BootstrapAsync(req);
        return resp.Skipped
            ? Ok(resp)
            : Created($"/api/v1/tenants/{resp.TenantId}/settings", resp);
    }
}
