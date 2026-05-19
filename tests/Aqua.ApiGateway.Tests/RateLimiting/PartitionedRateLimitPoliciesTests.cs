using System.Security.Claims;
using System.Threading.RateLimiting;
using Aqua.ApiGateway.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Aqua.ApiGateway.Tests.RateLimiting;

public class PartitionedRateLimitPoliciesTests
{
    [Fact]
    public void Per_tenant_partition_keys_by_HttpContext_Items_tenant()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["tenant"] = "acme";

        var partition = PartitionedRateLimitPolicies.PerTenant(ctx, limit: 100, windowSeconds: 60);

        partition.PartitionKey.Should().Be("acme");
    }

    [Fact]
    public void Per_tenant_returns_NoLimiter_when_tenant_missing()
    {
        var ctx = new DefaultHttpContext();
        var partition = PartitionedRateLimitPolicies.PerTenant(ctx, limit: 100, windowSeconds: 60);
        // No-limiter partitions return an "unlimited" key — anything non-throttling works.
        partition.PartitionKey.Should().Be("__no-tenant__");
    }

    [Fact]
    public void Per_user_partition_keys_by_sub_claim_when_authenticated()
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-42") }, "TestAuth"));

        var partition = PartitionedRateLimitPolicies.PerUser(ctx, limit: 100, windowSeconds: 60);
        partition.PartitionKey.Should().Be("user-42");
    }

    [Fact]
    public void Per_user_returns_NoLimiter_when_anonymous()
    {
        var ctx = new DefaultHttpContext();
        var partition = PartitionedRateLimitPolicies.PerUser(ctx, limit: 100, windowSeconds: 60);
        partition.PartitionKey.Should().Be("__anonymous__");
    }
}
