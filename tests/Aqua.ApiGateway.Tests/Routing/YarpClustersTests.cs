using Aqua.ApiGateway.Configuration;
using Aqua.ApiGateway.Routing;
using FluentAssertions;
using Xunit;

namespace Aqua.ApiGateway.Tests.Routing;

public class YarpClustersTests
{
    [Fact]
    public void Builds_one_cluster_and_route_per_service()
    {
        var opts = new GatewayOptions
        {
            JwtAuthority = "http://x", JwtAudience = "y",
            Services = new[]
            {
                new ServiceConfig { Name = "identity", BaseUrl = "http://identity:8080", PathPrefix = "/api/v1/auth", AnonymousPaths = new[] { "/token", "/refresh" } },
                new ServiceConfig { Name = "users", BaseUrl = "http://users:8080", PathPrefix = "/api/v1/users" },
            },
        };

        var (routes, clusters) = YarpClusters.Build(opts);

        clusters.Should().HaveCount(2);
        clusters.Select(c => c.ClusterId).Should().Contain(new[] { "identity", "users" });
        clusters.First(c => c.ClusterId == "identity").Destinations!.Values.Single().Address.Should().Be("http://identity:8080");

        routes.Should().HaveCount(4);
        routes.Should().Contain(r => r.RouteId == "identity:anonymous:token"    && r.AuthorizationPolicy == "Anonymous");
        routes.Should().Contain(r => r.RouteId == "identity:anonymous:refresh"  && r.AuthorizationPolicy == "Anonymous");
        routes.Should().Contain(r => r.RouteId == "identity:authenticated"      && r.AuthorizationPolicy == "RequireAuthenticatedUser");
        routes.Should().Contain(r => r.RouteId == "users:authenticated"         && r.AuthorizationPolicy == "RequireAuthenticatedUser");
    }

    [Fact]
    public void Wildcard_anonymous_path_creates_only_anonymous_route()
    {
        var opts = new GatewayOptions
        {
            JwtAuthority = "http://x", JwtAudience = "y",
            Services = new[]
            {
                new ServiceConfig { Name = "discovery", BaseUrl = "http://identity:8080", PathPrefix = "/.well-known", AnonymousPaths = new[] { "*" } },
            },
        };

        var (routes, _) = YarpClusters.Build(opts);

        routes.Should().ContainSingle();
        routes.Single().RouteId.Should().Be("discovery:anonymous");
        routes.Single().AuthorizationPolicy.Should().Be("Anonymous");
    }

    [Fact]
    public void Path_match_uses_catchall_with_prefix()
    {
        var opts = new GatewayOptions
        {
            JwtAuthority = "http://x", JwtAudience = "y",
            Services = new[]
            {
                new ServiceConfig { Name = "users", BaseUrl = "http://users:8080", PathPrefix = "/api/v1/users" },
            },
        };

        var (routes, _) = YarpClusters.Build(opts);

        routes.Single().Match.Path.Should().Be("/api/v1/users/{**catchall}");
    }
}
