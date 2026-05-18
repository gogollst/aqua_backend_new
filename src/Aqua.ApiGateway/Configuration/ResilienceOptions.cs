namespace Aqua.ApiGateway.Configuration;

public sealed class ResilienceOptions
{
    public int RetryAttempts { get; init; } = 2;
    public int RetryBaseDelayMilliseconds { get; init; } = 200;

    public double CircuitBreakerFailureRatio { get; init; } = 0.5;
    public int CircuitBreakerMinimumThroughput { get; init; } = 10;
    public int CircuitBreakerSamplingDurationSeconds { get; init; } = 30;
    public int CircuitBreakerBreakDurationSeconds { get; init; } = 15;

    public int RequestTimeoutSeconds { get; init; } = 30;
}
