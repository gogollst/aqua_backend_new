using Aqua.Contracts;
using Microsoft.AspNetCore.Http;

namespace Aqua.Data.Tenancy;

public sealed class TenantMiddleware
{
    private const string HeaderName = "X-Aqua-Tenant";
    private readonly RequestDelegate _next;
    private readonly ITenantContext _tenantContext;

    public TenantMiddleware(RequestDelegate next, ITenantContext tenantContext)
    {
        _next = next;
        _tenantContext = tenantContext;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var raw = values.ToString();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                _tenantContext.Set(new TenantId(raw));
            }
        }

        await _next(httpContext);
    }
}
