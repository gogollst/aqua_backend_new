using Microsoft.AspNetCore.Http;

namespace Aqua.ApiGateway.Headers;

/// <summary>
/// Inbound header policy. Strips any request header that is not on the explicit allow-list
/// before the request is forwarded downstream. Defends against header smuggling and limits
/// the surface area that downstream services need to defend against.
/// </summary>
public sealed class HeaderWhitelistMiddleware
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Accept", "Accept-Language", "Accept-Encoding",
        "Content-Type", "Content-Length",
        "User-Agent", "Referer",
        "X-Forwarded-For", "X-Forwarded-Proto", "X-Forwarded-Host",
        "X-Aqua-Tenant", "X-Correlation-Id",
        "If-None-Match", "If-Modified-Since",
        "Range",
        "Host",                          // ASP.NET Core relies on this internally; keep it.
        "Connection", "Upgrade",         // WebSocket upgrade pass-through (reserved for later WS support).
    };

    private readonly RequestDelegate _next;

    public HeaderWhitelistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var toRemove = context.Request.Headers.Keys
            .Where(k => !Allowed.Contains(k))
            .ToArray();

        foreach (var key in toRemove)
            context.Request.Headers.Remove(key);

        await _next(context);
    }
}
