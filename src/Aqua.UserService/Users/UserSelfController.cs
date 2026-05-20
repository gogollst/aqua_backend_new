using System.Security.Claims;
using Aqua.UserService.Domain;
using Aqua.UserService.Infrastructure;
using Aqua.UserService.Users.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Users;

/// <summary>
/// Self-service endpoints for the authenticated user. Read/patch own profile and
/// trigger a password change. Tenant is resolved from the X-Aqua-Tenant header via
/// <see cref="ITenantResolver"/>; the user id comes from the JWT <c>sub</c> claim.
/// </summary>
[ApiController]
[Route("api/v1/users/me")]
[Authorize]
public sealed class UserSelfController : ControllerBase
{
    private readonly IUserManager _users;
    private readonly ITenantResolver _tenants;

    public UserSelfController(IUserManager users, ITenantResolver tenants)
    {
        _users = users;
        _tenants = tenants;
    }

    [HttpGet]
    public async Task<ActionResult<UserDto>> Get() =>
        Ok(await _users.GetAsync(CurrentUserId()));

    [HttpPatch]
    public async Task<ActionResult<UserDto>> Patch(PatchUserRequest req)
    {
        var customerId = await _tenants.GetCurrentTenantIdAsync();
        return Ok(await _users.PatchAsync(CurrentUserId(), req, customerId));
    }

    [HttpPost("change-password")]
    public IActionResult ChangePassword(ChangePasswordRequest req)
    {
        // Password storage / verification lives in SS-05 (IdentityService). UserService deliberately
        // does NOT hold password material — so this endpoint is a contract placeholder that will
        // proxy to IdentityService once the inter-service call is wired (Task 32+).
        _ = req;
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    private long CurrentUserId()
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

public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);
