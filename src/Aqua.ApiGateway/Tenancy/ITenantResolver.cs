using Microsoft.AspNetCore.Http;

namespace Aqua.ApiGateway.Tenancy;

public interface ITenantResolver
{
    bool TryResolve(HttpContext httpContext, out string? tenant);
}
