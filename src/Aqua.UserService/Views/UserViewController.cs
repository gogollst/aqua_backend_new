using System.Security.Claims;
using Aqua.UserService.Domain;
using Aqua.UserService.Infrastructure;
using Aqua.UserService.Views.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Views;

/// <summary>
/// Self-service saved-views API. The owner is always the authenticated user — the
/// <c>sub</c> claim of the JWT identifies the user id; cross-user reads/writes are
/// not exposed.
/// </summary>
[ApiController]
[Route("api/v1/users/me/views")]
[Authorize]
public sealed class UserViewController : ControllerBase
{
    private readonly IUserViewManager _mgr;
    private readonly ITenantResolver _tenants;

    public UserViewController(IUserViewManager mgr, ITenantResolver tenants)
    {
        _mgr = mgr;
        _tenants = tenants;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserViewDto>>> List([FromQuery] long project) =>
        Ok(await _mgr.ListAsync(UserId(), project));

    [HttpPost]
    public async Task<ActionResult<UserViewDto>> Create(CreateUserViewRequest req)
    {
        var dto = await _mgr.CreateAsync(req, UserId(), await _tenants.GetCurrentTenantIdAsync());
        return CreatedAtAction(nameof(List), new { project = dto.ProjectId }, dto);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _mgr.DeleteAsync(id, UserId(), await _tenants.GetCurrentTenantIdAsync());
        return NoContent();
    }

    [HttpPut("{id:long}/favorite")]
    public async Task<IActionResult> SetFavorite(long id, [FromBody] SetFavoriteRequest req)
    {
        await _mgr.SetFavoriteAsync(id, UserId(), req.IsFavorite, await _tenants.GetCurrentTenantIdAsync());
        return NoContent();
    }

    public sealed record SetFavoriteRequest(bool IsFavorite);

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
