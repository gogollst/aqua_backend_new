using Aqua.Contracts;
using Aqua.Data.Tenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Aqua.Data.Tests;

public class TenantContextTests
{
    [Fact]
    public void TenantContext_DefaultsToNull()
    {
        var ctx = new TenantContext();
        ctx.Current.Should().BeNull();
    }

    [Fact]
    public void TenantContext_Set_StoresValue()
    {
        var ctx = new TenantContext();
        ctx.Set(new TenantId("acme"));
        ctx.Current!.Value.Value.Should().Be("acme");
    }

    [Fact]
    public async Task TenantMiddleware_ExtractsHeader()
    {
        var ctx = new TenantContext();
        var middleware = new TenantMiddleware(_ => Task.CompletedTask, ctx);
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Aqua-Tenant"] = "acme";

        await middleware.InvokeAsync(http);

        ctx.Current!.Value.Value.Should().Be("acme");
    }

    [Fact]
    public async Task TenantMiddleware_NoHeader_LeavesNull()
    {
        var ctx = new TenantContext();
        var middleware = new TenantMiddleware(_ => Task.CompletedTask, ctx);
        var http = new DefaultHttpContext();

        await middleware.InvokeAsync(http);

        ctx.Current.Should().BeNull();
    }
}
