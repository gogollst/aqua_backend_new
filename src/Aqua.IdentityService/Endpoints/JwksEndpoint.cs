using Aqua.IdentityService.Tokens;
using Microsoft.AspNetCore.Http;

namespace Aqua.IdentityService.Endpoints;

public static class JwksEndpoint
{
    public static IResult Handle(JwksProvider provider) => Results.Ok(provider.GetJwks());
}
