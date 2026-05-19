using System.Diagnostics.Metrics;

namespace Aqua.ApiGateway.Observability;

/// <summary>
/// Custom OpenTelemetry metric definitions for the gateway. Registered as a singleton via DI
/// so middleware can inject it and bump counters. Exporter wire-up is a deployment concern —
/// the meter is always created so emitting works regardless of whether an exporter is attached.
/// </summary>
public sealed class GatewayMeter : IDisposable
{
    public const string MeterName = "Aqua.ApiGateway";

    private readonly Meter _meter;
    public Counter<long> RateLimitRejected { get; }
    public Counter<long> CircuitBreakerStateChanges { get; }

    public GatewayMeter()
    {
        _meter = new Meter(MeterName, "1.0.0");
        RateLimitRejected = _meter.CreateCounter<long>(
            "aqua.gateway.ratelimit.rejected_total",
            unit: "requests",
            description: "Number of requests rejected by a rate-limit policy.");

        CircuitBreakerStateChanges = _meter.CreateCounter<long>(
            "aqua.gateway.circuitbreaker.state_change_total",
            unit: "events",
            description: "Number of circuit-breaker state-transition events per cluster.");
    }

    public void Dispose() => _meter.Dispose();
}
