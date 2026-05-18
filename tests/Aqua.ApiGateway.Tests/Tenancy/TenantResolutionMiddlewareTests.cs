using System.Text.Json;
using Aqua.ApiGateway.Configuration;
using Aqua.ApiGateway.Tenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aqua.ApiGateway.Tests.Tenancy;

public class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task Sets_TenantContext_when_any_resolver_succeeds()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Aqua-Tenant"] = "acme";

        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask, new ITenantResolver[]
        {
            new HeaderTenantResolver(Options.Create(new TenantResolutionOptions
            {
                Mode = TenantResolutionMode.Subdomain,
                SubdomainPattern = "x",
                DefaultTenant = "fallback",
            })),
        });

        await middleware.InvokeAsync(ctx);

        ctx.Items["tenant"].Should().Be("acme");
        ctx.Features.Get<TenantContext>()!.Slug.Should().Be("acme");
        ctx.Response.StatusCode.Should().NotBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Returns_400_with_ProblemDetails_when_no_resolver_matches()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask, Array.Empty<ITenantResolver>());

        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ctx.Response.ContentType.Should().StartWith("application/problem+json");

        ctx.Response.Body.Position = 0;
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        body.Should().Contain("/problems/tenant-required");
    }

    [Fact]
    public async Task First_successful_resolver_wins()
    {
        var ctx = new DefaultHttpContext();

        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask, new ITenantResolver[]
        {
            new StubResolver(tenant: "first"),
            new StubResolver(tenant: "second"),
        });

        await middleware.InvokeAsync(ctx);

        ctx.Items["tenant"].Should().Be("first");
    }

    private sealed class StubResolver(string? tenant) : ITenantResolver
    {
        public bool TryResolve(HttpContext _, out string? t) { t = tenant; return tenant is not null; }
    }
}
