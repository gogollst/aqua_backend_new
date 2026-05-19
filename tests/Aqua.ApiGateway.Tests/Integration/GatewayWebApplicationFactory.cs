using Aqua.ApiGateway.Configuration;
using Aqua.ApiGateway.OpenApi;
using Aqua.ApiGateway.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace Aqua.ApiGateway.Tests.Integration;

public sealed class GatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    public string IdentityBaseUrl { get; set; } = "http://127.0.0.1:9999";   // overridden per test
    public string JwtAuthority { get; set; }    = "http://127.0.0.1:9999";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gateway:JwtAuthority"] = JwtAuthority,
                ["Gateway:JwtAudience"]  = "aqua-api",
                ["Gateway:JwtRequireHttpsMetadata"] = "false",
                ["Gateway:Services:0:Name"]       = "identity",
                ["Gateway:Services:0:BaseUrl"]    = IdentityBaseUrl,
                ["Gateway:Services:0:PathPrefix"] = "/api/v1/auth",
                ["Gateway:Services:0:AnonymousPaths:0"] = "/token",
                ["Gateway:Services:0:AnonymousPaths:1"] = "/refresh",
                ["Gateway:Services:1:Name"]       = "users",
                ["Gateway:Services:1:BaseUrl"]    = IdentityBaseUrl,
                ["Gateway:Services:1:PathPrefix"] = "/api/v1/users",
                ["TenantResolution:Mode"]    = "Default",
                ["TenantResolution:DefaultTenant"] = "default",
                ["RateLimits:PerIp:PermitLimit"]   = "100",
                ["RateLimits:PerIp:WindowSeconds"] = "10",
            });
        });
        // Remove the OpenAPI background aggregation to prevent it from flooding
        // MockBackendFixture.ReceivedRequests with /openapi/v1.json GETs that
        // would make ReceivedRequests.Last() unreliable in header-propagation tests.
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ImplementationType == typeof(OpenApiAggregationHostedService));
            if (descriptor is not null) services.Remove(descriptor);
        });
    }

    // WebApplicationBuilder defers WebHost.ConfigureAppConfiguration until Build(), so the
    // GatewayOptions read by Program.cs at startup (before Build()) may see "Services: []" from
    // appsettings.json.  After the host is created the IOptions<GatewayOptions> IS correct;
    // we reload YARP's in-memory routes from it here.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        var gatewayOptions = host.Services.GetRequiredService<IOptions<GatewayOptions>>().Value;
        var (routes, clusters) = YarpClusters.Build(gatewayOptions);
        var configProvider = host.Services.GetRequiredService<InMemoryConfigProvider>();
        configProvider.Update(routes, clusters);

        return host;
    }
}
