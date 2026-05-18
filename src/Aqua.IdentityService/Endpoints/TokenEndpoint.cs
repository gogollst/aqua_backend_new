using Aqua.Contracts;
using Aqua.Contracts.Problems;
using Aqua.Data.Tenancy;
using Aqua.IdentityService.Authentication;
using Aqua.IdentityService.Domain;
using Aqua.IdentityService.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.IdentityService.Endpoints;

public static class TokenEndpoint
{
    public sealed record Request(string UserName, string Password);

    public static async Task<IResult> HandleAsync(
        [FromBody] Request body,
        IAuthenticationProvider auth,
        ITokenIssuer issuer,
        IRefreshTokenStore refreshStore,
        IUserRepository users,
        ITenantContext tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var current = tenant.Current ?? throw new InvalidOperationException("Tenant not resolved.");

        var result = await auth.AuthenticateAsync(body.UserName, body.Password, ct);
        if (!result.Success)
        {
            return Results.Json(new AquaProblemDetails
            {
                Type = ProblemTypes.Unauthorized,
                Title = "Authentication failed",
                Status = 401,
                Detail = result.FailureReason?.ToString(),
            }, statusCode: 401);
        }

        var user = await users.FindByUserNameAsync(result.UserName!, ct)
                   ?? throw new InvalidOperationException("User vanished after auth.");

        // Roles will be added in P1's UserService — empty for now.
        var pair = issuer.Issue(user.Id, user.UserName, current.Value, Array.Empty<string>());

        await refreshStore.SaveAsync(
            pair.RefreshToken,
            user.Id,
            current.Value,
            DateTimeOffset.UtcNow.AddDays(14),
            http.Connection.RemoteIpAddress?.ToString(),
            ct);

        return Results.Ok(new
        {
            access_token = pair.AccessToken,
            refresh_token = pair.RefreshToken,
            token_type = "Bearer",
            expires_at = pair.AccessTokenExpiresAt,
        });
    }
}
