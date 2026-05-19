using Microsoft.AspNetCore.Http;

namespace Aqua.ApiGateway.Headers;

/// <summary>
/// Outbound header enrichment. Adds X-Aqua-Tenant, X-Aqua-Original-User and X-Correlation-Id
/// to the request before YARP forwards it. Runs AFTER TenantResolutionMiddleware and the
/// JwtBearer authentication handler.
/// </summary>
public sealed class HeaderEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public HeaderEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Items["tenant"] is string tenant)
            context.Request.Headers["X-Aqua-Tenant"] = tenant;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(sub))
                context.Request.Headers["X-Aqua-Original-User"] = sub;
        }

        var correlationId = context.Request.Headers["X-Correlation-Id"].ToString();
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
            context.Request.Headers["X-Correlation-Id"] = correlationId;
        }
        context.Items["correlationId"] = correlationId;

        await _next(context);
    }
}
