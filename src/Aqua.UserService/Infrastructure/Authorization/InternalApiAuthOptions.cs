using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Aqua.UserService.Infrastructure.Authorization;

public sealed class InternalApiAuthOptions
{
    public string Token { get; init; } = "";
    public bool RequireMtls { get; init; } = false;
}

public sealed class InternalApiAuthSchemeOptions : AuthenticationSchemeOptions
{
    public IOptions<InternalApiAuthOptions> Options { get; set; } = default!;
}
