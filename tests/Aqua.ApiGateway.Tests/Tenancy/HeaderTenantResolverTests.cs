using Aqua.ApiGateway.Configuration;
using Aqua.ApiGateway.Tenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aqua.ApiGateway.Tests.Tenancy;

public class HeaderTenantResolverTests
{
    private static HeaderTenantResolver Build(string headerName = "X-Aqua-Tenant") =>
        new(Options.Create(new TenantResolutionOptions { Mode = TenantResolutionMode.Subdomain, SubdomainPattern = "x", HeaderName = headerName }));

    [Fact]
    public void Resolves_present_header()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Aqua-Tenant"] = "ACME";
        Build().TryResolve(ctx, out var tenant).Should().BeTrue();
        tenant.Should().Be("acme");
    }

    [Fact]
    public void Returns_false_when_header_missing()
    {
        Build().TryResolve(new DefaultHttpContext(), out _).Should().BeFalse();
    }

    [Fact]
    public void Returns_false_for_empty_header_value()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Aqua-Tenant"] = string.Empty;
        Build().TryResolve(ctx, out _).Should().BeFalse();
    }

    [Fact]
    public void Honors_custom_header_name()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Custom"] = "tenant1";
        Build("X-Custom").TryResolve(ctx, out var tenant).Should().BeTrue();
        tenant.Should().Be("tenant1");
    }
}

public class DefaultTenantResolverTests
{
    [Fact]
    public void Returns_configured_default_when_set()
    {
        var opts = Options.Create(new TenantResolutionOptions { Mode = TenantResolutionMode.Default, DefaultTenant = "single" });
        var sut = new DefaultTenantResolver(opts);
        sut.TryResolve(new DefaultHttpContext(), out var tenant).Should().BeTrue();
        tenant.Should().Be("single");
    }

    [Fact]
    public void Returns_false_when_default_not_set()
    {
        var opts = Options.Create(new TenantResolutionOptions { Mode = TenantResolutionMode.Subdomain, SubdomainPattern = "x", DefaultTenant = null });
        var sut = new DefaultTenantResolver(opts);
        sut.TryResolve(new DefaultHttpContext(), out _).Should().BeFalse();
    }
}
