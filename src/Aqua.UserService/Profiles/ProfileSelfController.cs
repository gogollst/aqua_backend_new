using System.Security.Claims;
using Aqua.UserService.Domain;
using Aqua.UserService.Infrastructure;
using Aqua.UserService.Profiles.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Profiles;

/// <summary>
/// Self-service profile endpoints — currently exposes the welcome-page config which the
/// web UI uses to drive the personal dashboard. Lives under /api/v1/users/me alongside
/// <see cref="Aqua.UserService.Users.UserSelfController"/>.
/// </summary>
[ApiController]
[Route("api/v1/users/me")]
[Authorize]
public sealed class ProfileSelfController : ControllerBase
{
    private readonly IProfileManager _profiles;
    private readonly ITenantResolver _tenants;

    public ProfileSelfController(IProfileManager profiles, ITenantResolver tenants)
    {
        _profiles = profiles;
        _tenants = tenants;
    }

    [HttpGet("welcome-config")]
    public async Task<ActionResult<WelcomePageConfigDto>> GetWelcome()
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        return Ok(await _profiles.GetWelcomeConfigAsync(UserId(), customerId));
    }

    [HttpPut("welcome-config")]
    public async Task<ActionResult<WelcomePageConfigDto>> PutWelcome(PutWelcomePageConfigRequest req)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        return Ok(await _profiles.PutWelcomeConfigAsync(UserId(), req, customerId));
    }

    private long UserId()
    {
        var sub = User.FindFirstValue("sub")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new ForbiddenException("auth.missing-sub", "No subject claim.");
        if (!long.TryParse(sub, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var id))
            throw new ForbiddenException("auth.bad-sub", $"Subject claim '{sub}' is not a numeric user id.");
        return id;
    }
}
