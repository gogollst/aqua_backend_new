using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aqua.UserService.Infrastructure.Authorization;

public sealed class InternalApiAuthHandler : AuthenticationHandler<InternalApiAuthSchemeOptions>
{
    public const string SchemeName = "InternalApi";
    public const string TokenHeader = "X-Internal-Token";

    public InternalApiAuthHandler(
        IOptionsMonitor<InternalApiAuthSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var opts = Options.Options.Value;

        if (opts.RequireMtls)
        {
            var clientCert = Context.Connection.ClientCertificate;
            if (clientCert is not null && clientCert.Verify())
            {
                return Task.FromResult(SucceedAsInternal(clientCert.Subject));
            }
            return Task.FromResult(AuthenticateResult.Fail("mTLS required but no valid client certificate"));
        }

        if (!Request.Headers.TryGetValue(TokenHeader, out var presented) || presented.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Internal-Token"));
        }
        if (!CryptographicEquals(presented.ToString(), opts.Token))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid X-Internal-Token"));
        }
        return Task.FromResult(SucceedAsInternal("token-caller"));
    }

    private AuthenticateResult SucceedAsInternal(string subject)
    {
        var claims = new[]
        {
            new Claim("internal-api", "true"),
            new Claim(ClaimTypes.Name, subject)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return AuthenticateResult.Success(ticket);
    }

    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
