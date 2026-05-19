using System.Security.Claims;
using Aqua.ApiGateway.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Aqua.ApiGateway.Tests.Headers;

public class HeaderEnrichmentMiddlewareTests
{
    [Fact]
    public async Task Sets_X_Aqua_Tenant_from_HttpContext_Items()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["tenant"] = "acme";

        var mw = new HeaderEnrichmentMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        ctx.Request.Headers["X-Aqua-Tenant"].ToString().Should().Be("acme");
    }

    [Fact]
    public async Task Sets_X_Aqua_Original_User_from_sub_claim()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["tenant"] = "acme";
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-42") }, "TestAuth"));

        var mw = new HeaderEnrichmentMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        ctx.Request.Headers["X-Aqua-Original-User"].ToString().Should().Be("user-42");
    }

    [Fact]
    public async Task Skips_X_Aqua_Original_User_when_anonymous()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["tenant"] = "acme";

        var mw = new HeaderEnrichmentMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        ctx.Request.Headers.Should().NotContainKey("X-Aqua-Original-User");
    }

    [Fact]
    public async Task Uses_existing_correlation_id_when_provided()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["tenant"] = "acme";
        ctx.Request.Headers["X-Correlation-Id"] = "client-supplied-id";

        var mw = new HeaderEnrichmentMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        ctx.Request.Headers["X-Correlation-Id"].ToString().Should().Be("client-supplied-id");
    }

    [Fact]
    public async Task Generates_new_correlation_id_when_missing()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["tenant"] = "acme";

        var mw = new HeaderEnrichmentMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        var corr = ctx.Request.Headers["X-Correlation-Id"].ToString();
        corr.Should().NotBeNullOrEmpty();
        Guid.TryParse(corr, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Stores_correlation_id_in_HttpContext_Items_for_logging()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["tenant"] = "acme";

        var mw = new HeaderEnrichmentMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        ctx.Items["correlationId"].Should().BeOfType<string>().Which.Should().NotBeNullOrEmpty();
    }
}
