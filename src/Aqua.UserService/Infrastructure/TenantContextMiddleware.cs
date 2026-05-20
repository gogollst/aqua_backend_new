using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aqua.UserService.Infrastructure;

public sealed class TenantContextMiddleware
{
    public const string TenantHeader = "X-Aqua-Tenant";
    private static readonly string[] ExemptPrefixes =
    {
        "/internal/v1/",
        "/admin/cross-tenant/",
        "/healthz",
        "/readyz",
        "/openapi",
        "/swagger"
    };

    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";
        var tenant = ctx.RequestServices.GetRequiredService<CurrentTenant>();

        if (ctx.Request.Headers.TryGetValue(TenantHeader, out var slug) && !string.IsNullOrWhiteSpace(slug))
        {
            tenant.Set(slug.ToString());
            await _next(ctx);
            return;
        }

        if (ExemptPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal)))
        {
            await _next(ctx);
            return;
        }

        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsJsonAsync(new
        {
            type   = "https://aqua-cloud.io/problems/tenant.missing",
            title  = "Tenant header missing",
            status = 400,
            detail = $"Request to {path} requires X-Aqua-Tenant header."
        }, options: null, contentType: "application/problem+json");
    }
}
