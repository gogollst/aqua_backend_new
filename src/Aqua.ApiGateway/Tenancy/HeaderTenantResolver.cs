using Aqua.ApiGateway.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Aqua.ApiGateway.Tenancy;

public sealed class HeaderTenantResolver : ITenantResolver
{
    private readonly string _headerName;

    public HeaderTenantResolver(IOptions<TenantResolutionOptions> options)
    {
        _headerName = options.Value.HeaderName;
    }

    public bool TryResolve(HttpContext httpContext, out string? tenant)
    {
        tenant = null;
        if (!httpContext.Request.Headers.TryGetValue(_headerName, out var values)) return false;

        var value = values.ToString();
        if (string.IsNullOrWhiteSpace(value)) return false;

        tenant = value.Trim().ToLowerInvariant();
        return true;
    }
}
