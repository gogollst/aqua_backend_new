using Aqua.UserService.ClaimsLookup.Dto;
using Aqua.UserService.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.ClaimsLookup;

[ApiController]
[Route("internal/v1/users")]
[Authorize(AuthenticationSchemes = InternalApiAuthHandler.SchemeName)]
public sealed class InternalClaimsController : ControllerBase
{
    private readonly IClaimsLookupService _svc;
    public InternalClaimsController(IClaimsLookupService svc) => _svc = svc;

    [HttpGet("{id:long}/claims")]
    public async Task<ActionResult<ClaimsLookupResponse>> Get(long id, [FromQuery] string tenant)
        => Ok(await _svc.LookupAsync(id, tenant));
}
