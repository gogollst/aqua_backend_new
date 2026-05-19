using Aqua.ApiGateway.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Aqua.ApiGateway.HealthChecks;

public sealed class JwksReachabilityHealthCheck : IHealthCheck
{
    public const string Name = "jwks-reachability";

    private readonly HttpClient _httpClient;
    private readonly string _jwksUrl;

    public JwksReachabilityHealthCheck(HttpClient httpClient, IOptions<GatewayOptions> options)
    {
        _httpClient = httpClient;
        _jwksUrl = options.Value.JwtAuthority.TrimEnd('/') + "/.well-known/jwks.json";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var response = await _httpClient.GetAsync(_jwksUrl, cts.Token);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"JWKS responded {(int)response.StatusCode}")
                : HealthCheckResult.Unhealthy($"JWKS returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("JWKS unreachable", ex);
        }
    }
}
