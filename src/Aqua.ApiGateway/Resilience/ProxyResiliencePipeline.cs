using System.Net;
using Aqua.ApiGateway.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Aqua.ApiGateway.Resilience;

public static class ProxyResiliencePipeline
{
    public const string PipelineName = "aqua-gateway-pipeline";

    /// <summary>
    /// Adds Retry + CircuitBreaker + Timeout to the default <c>HttpClient</c> used by YARP
    /// for backend cluster forwarding. Each YARP cluster gets its own HttpClient instance,
    /// so the circuit-breaker state is isolated per cluster automatically.
    /// </summary>
    public static IServiceCollection AddProxyResilience(this IServiceCollection services)
    {
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddResilienceHandler(PipelineName, (builder, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<ResilienceOptions>>().Value;

                builder
                    .AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = options.RetryAttempts,
                        Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMilliseconds),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        ShouldHandle = static args => ValueTask.FromResult(args.Outcome switch
                        {
                            { Result.StatusCode: HttpStatusCode.ServiceUnavailable } => true,
                            { Result.StatusCode: HttpStatusCode.GatewayTimeout }     => true,
                            { Exception: HttpRequestException }                       => true,
                            { Exception: TaskCanceledException }                      => true,
                            _ => false,
                        }),
                    })
                    .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = options.CircuitBreakerFailureRatio,
                        MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                        SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingDurationSeconds),
                        BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds),
                    })
                    .AddTimeout(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
            });
        });

        return services;
    }
}
