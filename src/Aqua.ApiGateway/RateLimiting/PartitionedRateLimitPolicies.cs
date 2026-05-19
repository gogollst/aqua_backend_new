using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Aqua.ApiGateway.RateLimiting;

/// <summary>
/// Partition-key factories for the post-auth rate-limit policies.
/// Wired into <c>RateLimiterOptions</c> in <c>Program.cs</c> via
/// <c>opts.AddPolicy("per-tenant", ctx =&gt; PartitionedRateLimitPolicies.PerTenant(...));</c>
/// </summary>
public static class PartitionedRateLimitPolicies
{
    public const string PerTenantPolicyName = "per-tenant";
    public const string PerUserPolicyName   = "per-user";

    public static RateLimitPartition<string> PerTenant(HttpContext context, int limit, int windowSeconds)
    {
        var tenant = context.Items["tenant"] as string;
        if (string.IsNullOrEmpty(tenant))
            return RateLimitPartition.GetNoLimiter("__no-tenant__");

        return RateLimitPartition.GetSlidingWindowLimiter(tenant, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = limit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            SegmentsPerWindow = 6,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    }

    public static RateLimitPartition<string> PerUser(HttpContext context, int limit, int windowSeconds)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return RateLimitPartition.GetNoLimiter("__anonymous__");

        var sub = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub))
            return RateLimitPartition.GetNoLimiter("__anonymous__");

        return RateLimitPartition.GetSlidingWindowLimiter(sub, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = limit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            SegmentsPerWindow = 6,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    }
}
