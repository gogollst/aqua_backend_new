using System.Text.Json;
using System.Threading.RateLimiting;
using Aqua.ApiGateway.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Aqua.ApiGateway.RateLimiting;

public sealed class PerIpRateLimitMiddleware : IAsyncDisposable
{
    private readonly RequestDelegate _next;
    private readonly PartitionedRateLimiter<HttpContext> _limiter;
    private readonly int _windowSeconds;

    public PerIpRateLimitMiddleware(RequestDelegate next, IOptions<RateLimitOptions> options)
    {
        _next = next;
        var settings = options.Value.PerIp;
        _windowSeconds = settings.WindowSeconds;

        _limiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        {
            var key = ExtractIp(ctx);
            return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            });
        });
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var lease = await _limiter.AcquireAsync(context, permitCount: 1, context.RequestAborted);
        if (lease.IsAcquired)
        {
            await _next(context);
            return;
        }

        await WriteRateLimitedAsync(context, _windowSeconds, "per-ip");
    }

    private static string ExtractIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var first = forwarded.ToString().Split(',', 2)[0].Trim();
            if (!string.IsNullOrEmpty(first)) return first;
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    internal static async Task WriteRateLimitedAsync(HttpContext context, int windowSeconds, string policy)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers["Retry-After"] = windowSeconds.ToString();
        context.Response.ContentType = "application/problem+json";
        var body = JsonSerializer.Serialize(new
        {
            type = "/problems/rate-limited",
            title = "Too many requests",
            status = 429,
            detail = $"Rate limit for policy '{policy}' was exceeded.",
            policy,
        });
        await context.Response.WriteAsync(body);
    }

    public ValueTask DisposeAsync() => _limiter.DisposeAsync();
}
