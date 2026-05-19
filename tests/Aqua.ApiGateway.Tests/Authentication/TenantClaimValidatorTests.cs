using System.Security.Claims;
using Aqua.ApiGateway.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Aqua.ApiGateway.Tests.Authentication;

public class TenantClaimValidatorTests
{
    [Fact]
    public async Task Passes_through_when_tenant_matches_claim()
    {
        var ctx = BuildContext(itemsTenant: "acme", claimTenant: "acme", authenticated: true);
        var called = false;
        var mw = new TenantClaimValidator(_ => { called = true; return Task.CompletedTask; });

        await mw.InvokeAsync(ctx);

        called.Should().BeTrue();
        ctx.Response.StatusCode.Should().NotBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Returns_403_when_tenant_claim_mismatches()
    {
        var ctx = BuildContext(itemsTenant: "acme", claimTenant: "rival-corp", authenticated: true);
        ctx.Response.Body = new MemoryStream();
        var mw = new TenantClaimValidator(_ => Task.CompletedTask);

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ctx.Response.ContentType.Should().StartWith("application/problem+json");
        ctx.Response.Body.Position = 0;
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        body.Should().Contain("/problems/tenant-mismatch");
    }

    [Fact]
    public async Task Skips_validation_for_anonymous_request()
    {
        var ctx = BuildContext(itemsTenant: "acme", claimTenant: null, authenticated: false);
        var called = false;
        var mw = new TenantClaimValidator(_ => { called = true; return Task.CompletedTask; });

        await mw.InvokeAsync(ctx);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task Returns_403_when_authenticated_but_tenant_claim_missing()
    {
        var ctx = BuildContext(itemsTenant: "acme", claimTenant: null, authenticated: true);
        ctx.Response.Body = new MemoryStream();
        var mw = new TenantClaimValidator(_ => Task.CompletedTask);

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    private static DefaultHttpContext BuildContext(string itemsTenant, string? claimTenant, bool authenticated)
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["tenant"] = itemsTenant;
        var claims = new List<Claim>();
        if (claimTenant is not null) claims.Add(new Claim("tenant", claimTenant));
        var identity = new ClaimsIdentity(claims, authenticated ? "TestAuth" : null);
        ctx.User = new ClaimsPrincipal(identity);
        return ctx;
    }
}
