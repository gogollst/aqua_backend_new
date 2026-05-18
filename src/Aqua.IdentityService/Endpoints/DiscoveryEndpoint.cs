using Aqua.IdentityService.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Aqua.IdentityService.Endpoints;

public static class DiscoveryEndpoint
{
    public static IResult Handle(IOptions<IdentityOptions> options, HttpContext http)
    {
        var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
        return Results.Ok(new
        {
            issuer = options.Value.Issuer,
            jwks_uri = $"{baseUrl}/.well-known/jwks.json",
            token_endpoint = $"{baseUrl}/api/v1/auth/token",
            grant_types_supported = new[] { "password", "refresh_token" },
            response_types_supported = new[] { "token" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            subject_types_supported = new[] { "public" },
            claims_supported = new[] { "sub", "user_name", "tenant", "roles", "jti", "iss", "aud", "exp", "iat", "nbf" },
        });
    }
}
