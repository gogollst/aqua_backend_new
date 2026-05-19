using Aqua.ApiGateway.Configuration;
using Yarp.ReverseProxy.Configuration;

namespace Aqua.ApiGateway.Routing;

public static class YarpClusters
{
    public const string AnonymousPolicy             = "Anonymous";
    public const string RequireAuthenticatedPolicy  = "RequireAuthenticatedUser";

    /// <summary>
    /// Translates the <c>GatewayOptions.Services</c> list into YARP's <c>RouteConfig</c>/<c>ClusterConfig</c>
    /// records, splitting each service into up to two routes (one anonymous, one authenticated) based on
    /// the <c>AnonymousPaths</c> declarations.
    /// </summary>
    public static (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Build(GatewayOptions options)
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var svc in options.Services)
        {
            clusters.Add(new ClusterConfig
            {
                ClusterId = svc.Name,
                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                {
                    [$"{svc.Name}-primary"] = new() { Address = svc.BaseUrl },
                },
            });

            var hasWildcardAnonymous = svc.AnonymousPaths.Any(p => p == "*");

            if (hasWildcardAnonymous)
            {
                routes.Add(BuildRoute(svc, anonymous: true, suffix: null));
                continue;
            }

            if (svc.AnonymousPaths.Count > 0)
            {
                foreach (var anonymousSubPath in svc.AnonymousPaths)
                    routes.Add(BuildRoute(svc, anonymous: true, suffix: anonymousSubPath));
            }

            // Authenticated catch-all is always present unless wildcard-anonymous applied.
            routes.Add(BuildRoute(svc, anonymous: false, suffix: null));
        }

        return (routes, clusters);
    }

    private static RouteConfig BuildRoute(ServiceConfig svc, bool anonymous, string? suffix)
    {
        var routeId = anonymous
            ? (suffix is null ? $"{svc.Name}:anonymous" : $"{svc.Name}:anonymous:{suffix.Trim('/')}")
            : $"{svc.Name}:authenticated";

        var pathTemplate = suffix is null
            ? $"{svc.PathPrefix.TrimEnd('/')}/{{**catchall}}"
            : $"{svc.PathPrefix.TrimEnd('/')}{suffix}";

        return new RouteConfig
        {
            RouteId = routeId,
            ClusterId = svc.Name,
            AuthorizationPolicy = anonymous ? AnonymousPolicy : RequireAuthenticatedPolicy,
            Match = new RouteMatch { Path = pathTemplate },
        };
    }
}
