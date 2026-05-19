using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Aqua.ApiGateway.Authentication;

/// <summary>
/// Defense-in-Depth: cross-checks the tenant that <c>TenantResolutionMiddleware</c> placed in
/// <c>HttpContext.Items["tenant"]</c> against the <c>tenant</c> claim of the authenticated JWT.
/// Anonymous requests (no authenticated principal) are passed through — auth is enforced
/// elsewhere by the JwtBearer handler / authorization policies.
/// </summary>
public sealed class TenantClaimValidator
{
    private readonly RequestDelegate _next;

    public TenantClaimValidator(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var resolved = context.Items["tenant"] as string;
        var claim = context.User.FindFirst("tenant")?.Value;

        if (resolved is null || claim is null ||
            !string.Equals(resolved, claim, StringComparison.OrdinalIgnoreCase))
        {
            await WriteForbiddenAsync(context);
            return;
        }

        await _next(context);
    }

    private static async Task WriteForbiddenAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        var body = JsonSerializer.Serialize(new
        {
            type = "/problems/tenant-mismatch",
            title = "Tenant claim does not match resolved tenant",
            status = 403,
            detail = "The JWT's tenant claim does not match the tenant resolved from the request host or header.",
        });
        await context.Response.WriteAsync(body);
    }
}
