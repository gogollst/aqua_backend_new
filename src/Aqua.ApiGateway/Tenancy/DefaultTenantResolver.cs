using Aqua.ApiGateway.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Aqua.ApiGateway.Tenancy;

public sealed class DefaultTenantResolver : ITenantResolver
{
    private readonly string? _default;

    public DefaultTenantResolver(IOptions<TenantResolutionOptions> options)
    {
        _default = options.Value.DefaultTenant;
    }

    public bool TryResolve(HttpContext httpContext, out string? tenant)
    {
        tenant = _default;
        return !string.IsNullOrWhiteSpace(_default);
    }
}
