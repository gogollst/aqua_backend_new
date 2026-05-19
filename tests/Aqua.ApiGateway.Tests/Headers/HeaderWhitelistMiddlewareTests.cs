using Aqua.ApiGateway.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Aqua.ApiGateway.Tests.Headers;

public class HeaderWhitelistMiddlewareTests
{
    [Fact]
    public async Task Strips_non_whitelisted_headers()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Authorization"] = "Bearer abc";
        ctx.Request.Headers["X-Aqua-Tenant"] = "acme";
        ctx.Request.Headers["X-Custom-Hack"] = "value";
        ctx.Request.Headers["X-Debug-Mode"] = "1";

        var mw = new HeaderWhitelistMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        ctx.Request.Headers.Should().ContainKey("Authorization");
        ctx.Request.Headers.Should().ContainKey("X-Aqua-Tenant");
        ctx.Request.Headers.Should().NotContainKey("X-Custom-Hack");
        ctx.Request.Headers.Should().NotContainKey("X-Debug-Mode");
    }

    [Fact]
    public async Task Keeps_standard_browser_headers()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Accept"] = "application/json";
        ctx.Request.Headers["Accept-Language"] = "de-DE";
        ctx.Request.Headers["Accept-Encoding"] = "gzip";
        ctx.Request.Headers["Content-Type"] = "application/json";
        ctx.Request.Headers["User-Agent"] = "test";
        ctx.Request.Headers["Referer"] = "https://example.com";

        var mw = new HeaderWhitelistMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        foreach (var name in new[] { "Accept", "Accept-Language", "Accept-Encoding", "Content-Type", "User-Agent", "Referer" })
            ctx.Request.Headers.Should().ContainKey(name, $"because {name} is on the whitelist");
    }

    [Fact]
    public async Task Keeps_aqua_and_forwarded_headers()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Aqua-Tenant"] = "acme";
        ctx.Request.Headers["X-Correlation-Id"] = "abc";
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4";
        ctx.Request.Headers["X-Forwarded-Proto"] = "https";
        ctx.Request.Headers["X-Forwarded-Host"] = "acme.aqua-cloud.io";

        var mw = new HeaderWhitelistMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        foreach (var name in new[] { "X-Aqua-Tenant", "X-Correlation-Id", "X-Forwarded-For", "X-Forwarded-Proto", "X-Forwarded-Host" })
            ctx.Request.Headers.Should().ContainKey(name);
    }
}
