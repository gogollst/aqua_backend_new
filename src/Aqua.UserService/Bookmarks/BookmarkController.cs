using System.Security.Claims;
using Aqua.UserService.Bookmarks.Dto;
using Aqua.UserService.Domain;
using Aqua.UserService.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Bookmarks;

/// <summary>
/// Self-service bookmark API. Owner identity comes from the JWT <c>sub</c> claim;
/// the manager enforces single-tenant scoping and owner checks on delete.
/// </summary>
[ApiController]
[Route("api/v1/users/me/bookmarks")]
[Authorize]
public sealed class BookmarkController : ControllerBase
{
    private readonly IBookmarkManager _mgr;
    private readonly ITenantResolver _tenants;

    public BookmarkController(IBookmarkManager mgr, ITenantResolver tenants)
    {
        _mgr = mgr;
        _tenants = tenants;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookmarkDto>>> List([FromQuery] long project) =>
        Ok(await _mgr.ListAsync(UserId(), project));

    [HttpPost]
    public async Task<ActionResult<BookmarkDto>> Create(CreateBookmarkRequest req)
    {
        var dto = await _mgr.CreateAsync(req, UserId(), await _tenants.GetCurrentTenantIdAsync());
        return Created($"/api/v1/users/me/bookmarks/{dto.Id}", dto);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _mgr.DeleteAsync(id, UserId(), await _tenants.GetCurrentTenantIdAsync());
        return NoContent();
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
