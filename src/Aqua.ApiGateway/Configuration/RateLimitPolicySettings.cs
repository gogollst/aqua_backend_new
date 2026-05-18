namespace Aqua.ApiGateway.Configuration;

public sealed record RateLimitPolicySettings(int PermitLimit, int WindowSeconds);
