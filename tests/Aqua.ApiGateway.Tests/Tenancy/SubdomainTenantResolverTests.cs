using Aqua.ApiGateway.Configuration;
using Aqua.ApiGateway.Tenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aqua.ApiGateway.Tests.Tenancy;

public class SubdomainTenantResolverTests
{
    private static SubdomainTenantResolver Build(string pattern, params string[] reserved)
    {
        var opts = new TenantResolutionOptions
        {
            Mode = TenantResolutionMode.Subdomain,
            SubdomainPattern = pattern,
            ReservedSubdomains = reserved,
        };
        return new SubdomainTenantResolver(Options.Create(opts));
    }

    [Theory]
    [InlineData("acme.aqua-cloud.io", "acme")]
    [InlineData("ACME.aqua-cloud.io",  "acme")]
    [InlineData("Acme-Corp.aqua-cloud.io", "acme-corp")]
    public void Resolves_valid_subdomain(string host, string expected)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString(host);
        var sut = Build(@"^([a-z0-9][a-z0-9-]{1,30})\.aqua-cloud\.io$");
        sut.TryResolve(ctx, out var tenant).Should().BeTrue();
        tenant.Should().Be(expected);
    }

    [Theory]
    [InlineData("www.aqua-cloud.io")]
    [InlineData("api.aqua-cloud.io")]
    [InlineData("admin.aqua-cloud.io")]
    public void Reserved_subdomains_do_not_resolve(string host)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString(host);
        var sut = Build(@"^([a-z0-9][a-z0-9-]{1,30})\.aqua-cloud\.io$", "www", "api", "admin");
        sut.TryResolve(ctx, out var tenant).Should().BeFalse();
        tenant.Should().BeNull();
    }

    [Fact]
    public void Host_without_match_returns_false()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("not-an-aqua-host.example.com");
        var sut = Build(@"^([a-z0-9][a-z0-9-]{1,30})\.aqua-cloud\.io$");
        sut.TryResolve(ctx, out _).Should().BeFalse();
    }

    [Fact]
    public void Host_with_port_strips_port_before_match()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("acme.aqua-cloud.io", 8443);
        var sut = Build(@"^([a-z0-9][a-z0-9-]{1,30})\.aqua-cloud\.io$");
        sut.TryResolve(ctx, out var tenant).Should().BeTrue();
        tenant.Should().Be("acme");
    }
}
