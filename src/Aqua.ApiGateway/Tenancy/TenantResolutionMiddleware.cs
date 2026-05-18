using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Aqua.ApiGateway.Tenancy;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IReadOnlyList<ITenantResolver> _resolvers;

    public TenantResolutionMiddleware(RequestDelegate next, IEnumerable<ITenantResolver> resolvers)
    {
        _next = next;
        _resolvers = resolvers.ToArray();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        foreach (var resolver in _resolvers)
        {
            if (resolver.TryResolve(context, out var tenant) && tenant is not null)
            {
                context.Items["tenant"] = tenant;
                context.Features.Set(new TenantContext(tenant));
                await _next(context);
                return;
            }
        }

        await WriteTenantRequiredAsync(context);
    }

    private static async Task WriteTenantRequiredAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        var body = JsonSerializer.Serialize(new
        {
            type = "/problems/tenant-required",
            title = "Tenant could not be resolved",
            status = 400,
            detail = "Request did not specify a tenant via subdomain, header, or default configuration.",
        });
        await context.Response.WriteAsync(body);
    }
}
