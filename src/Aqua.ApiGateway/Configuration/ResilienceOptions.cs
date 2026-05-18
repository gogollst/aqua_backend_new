using System.ComponentModel.DataAnnotations;

namespace Aqua.ApiGateway.Configuration;

public sealed class ResilienceOptions
{
    [Range(0, 10)]
    public int RetryAttempts { get; init; } = 2;

    [Range(0, 60000)]
    public int RetryBaseDelayMilliseconds { get; init; } = 200;

    [Range(0.0, 1.0)]
    public double CircuitBreakerFailureRatio { get; init; } = 0.5;

    [Range(1, 1000)]
    public int CircuitBreakerMinimumThroughput { get; init; } = 10;

    [Range(1, 3600)]
    public int CircuitBreakerSamplingDurationSeconds { get; init; } = 30;

    [Range(1, 3600)]
    public int CircuitBreakerBreakDurationSeconds { get; init; } = 15;

    [Range(1, 600)]
    public int RequestTimeoutSeconds { get; init; } = 30;
}
