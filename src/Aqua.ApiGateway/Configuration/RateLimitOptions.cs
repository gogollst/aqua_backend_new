namespace Aqua.ApiGateway.Configuration;

public sealed class RateLimitOptions
{
    public RateLimitPolicySettings PerIp { get; init; }     = new(PermitLimit: 100,  WindowSeconds: 10);
    public RateLimitPolicySettings PerTenant { get; init; } = new(PermitLimit: 1000, WindowSeconds: 60);
    public RateLimitPolicySettings PerUser { get; init; }   = new(PermitLimit: 600,  WindowSeconds: 60);
}
