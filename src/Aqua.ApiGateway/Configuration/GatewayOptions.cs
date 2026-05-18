using System.ComponentModel.DataAnnotations;

namespace Aqua.ApiGateway.Configuration;

public sealed class GatewayOptions
{
    [Required] public required string JwtAuthority { get; init; }
    [Required] public required string JwtAudience { get; init; }
    public bool JwtRequireHttpsMetadata { get; init; } = false;

    [MinLength(1, ErrorMessage = "At least one downstream service must be configured.")]
    public IReadOnlyList<ServiceConfig> Services { get; init; } = Array.Empty<ServiceConfig>();
}

public sealed class ServiceConfig
{
    [Required] public required string Name { get; init; }
    [Required, Url] public required string BaseUrl { get; init; }
    [Required] public required string PathPrefix { get; init; }
    public IReadOnlyList<string> AnonymousPaths { get; init; } = Array.Empty<string>();
}
