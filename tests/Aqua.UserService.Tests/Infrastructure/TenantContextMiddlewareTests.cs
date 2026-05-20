using Aqua.UserService.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aqua.UserService.Tests.Infrastructure;

public sealed class TenantContextMiddlewareTests
{
    [Fact]
    public async Task Sets_tenant_when_header_present()
    {
        var (mw, http, tenant) = Build("acme");
        await mw.InvokeAsync(http);
        tenant.Slug.Should().Be("acme");
        tenant.IsResolved.Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/v1/users")]
    [InlineData("/api/v1/roles/1")]
    public async Task Returns_400_when_header_missing_for_public_api(string path)
    {
        var (mw, http, _) = Build(slugHeader: null, path: path);
        await mw.InvokeAsync(http);
        http.Response.StatusCode.Should().Be(400);
    }

    [Theory]
    [InlineData("/internal/v1/users/1/claims")]
    [InlineData("/admin/cross-tenant/users")]
    [InlineData("/healthz")]
    public async Task Allows_missing_header_for_internal_or_health(string path)
    {
        var (mw, http, _) = Build(slugHeader: null, path: path);
        await mw.InvokeAsync(http);
        http.Response.StatusCode.Should().NotBe(400);
    }

    private static (TenantContextMiddleware mw, HttpContext http, CurrentTenant tenant) Build(
        string? slugHeader = "acme", string path = "/api/v1/users")
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        if (slugHeader is not null) http.Request.Headers["X-Aqua-Tenant"] = slugHeader;
        var tenant = new CurrentTenant();
        var services = new ServiceCollection();
        services.AddSingleton(tenant);
        http.RequestServices = services.BuildServiceProvider();
        var mw = new TenantContextMiddleware(_ => Task.CompletedTask);
        return (mw, http, tenant);
    }
}
