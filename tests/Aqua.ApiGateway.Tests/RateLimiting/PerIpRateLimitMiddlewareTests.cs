using System.Net;
using Aqua.ApiGateway.Configuration;
using Aqua.ApiGateway.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aqua.ApiGateway.Tests.RateLimiting;

public class PerIpRateLimitMiddlewareTests
{
    private static PerIpRateLimitMiddleware Build(int permitLimit, int windowSeconds, RequestDelegate next) =>
        new(next, Options.Create(new RateLimitOptions
        {
            PerIp = new RateLimitPolicySettings(permitLimit, windowSeconds),
        }));

    [Fact]
    public async Task Allows_requests_within_limit()
    {
        var called = 0;
        var mw = Build(3, 60, _ => { called++; return Task.CompletedTask; });

        for (var i = 0; i < 3; i++)
            await mw.InvokeAsync(Ctx("1.2.3.4"));

        called.Should().Be(3);
    }

    [Fact]
    public async Task Returns_429_when_limit_exceeded()
    {
        var mw = Build(2, 60, _ => Task.CompletedTask);

        await mw.InvokeAsync(Ctx("1.2.3.4"));
        await mw.InvokeAsync(Ctx("1.2.3.4"));
        var third = Ctx("1.2.3.4");
        third.Response.Body = new MemoryStream();

        await mw.InvokeAsync(third);

        third.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        third.Response.Headers.Should().ContainKey("Retry-After");
        third.Response.ContentType.Should().StartWith("application/problem+json");
    }

    [Fact]
    public async Task Different_ips_have_independent_limits()
    {
        var mw = Build(1, 60, _ => Task.CompletedTask);
        await mw.InvokeAsync(Ctx("1.1.1.1"));
        await mw.InvokeAsync(Ctx("2.2.2.2"));
        var third = Ctx("2.2.2.2");
        await mw.InvokeAsync(third);
        third.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task Uses_X_Forwarded_For_first_hop_when_present()
    {
        var mw = Build(1, 60, _ => Task.CompletedTask);

        var first = Ctx(remoteIp: "10.0.0.1", xForwardedFor: "203.0.113.5, 10.0.0.1");
        var second = Ctx(remoteIp: "10.0.0.1", xForwardedFor: "203.0.113.5, 10.0.0.1");

        await mw.InvokeAsync(first);
        await mw.InvokeAsync(second);

        second.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    private static DefaultHttpContext Ctx(string remoteIp, string? xForwardedFor = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        if (xForwardedFor is not null)
            ctx.Request.Headers["X-Forwarded-For"] = xForwardedFor;
        return ctx;
    }
}
