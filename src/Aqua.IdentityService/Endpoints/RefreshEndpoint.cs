using Aqua.Contracts.Problems;
using Aqua.Data.Tenancy;
using Aqua.IdentityService.Domain;
using Aqua.IdentityService.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.IdentityService.Endpoints;

public static class RefreshEndpoint
{
    public sealed record Request(string RefreshToken);

    public static async Task<IResult> HandleAsync(
        [FromBody] Request body,
        ITokenIssuer issuer,
        IRefreshTokenStore refreshStore,
        IUserRepository users,
        ITenantContext tenant,
        CancellationToken ct)
    {
        var current = tenant.Current ?? throw new InvalidOperationException("Tenant not resolved.");

        // Generate a new pair first so we have an ID to record into rotated_to_token_id.
        // We need the user from the consumed token, so peek first.
        var consumed = await refreshStore.ConsumeAsync(body.RefreshToken, Guid.NewGuid(), ct);
        if (consumed is null)
        {
            return Results.Json(new AquaProblemDetails
            {
                Type = ProblemTypes.Unauthorized,
                Title = "Refresh token invalid or expired",
                Status = 401,
            }, statusCode: 401);
        }

        var user = await users.GetByIdAsync(consumed.UserId, ct);
        if (user is null || user.Deleted)
        {
            return Results.Json(new AquaProblemDetails
            {
                Type = ProblemTypes.Unauthorized,
                Title = "User no longer valid",
                Status = 401,
            }, statusCode: 401);
        }

        var newPair = issuer.Issue(user.Id, user.UserName, current.Value, Array.Empty<string>());

        await refreshStore.SaveAsync(
            newPair.RefreshToken,
            user.Id,
            current.Value,
            DateTimeOffset.UtcNow.AddDays(14),
            null,
            ct);

        return Results.Ok(new
        {
            access_token = newPair.AccessToken,
            refresh_token = newPair.RefreshToken,
            token_type = "Bearer",
            expires_at = newPair.AccessTokenExpiresAt,
        });
    }
}
