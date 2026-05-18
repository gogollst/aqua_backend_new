using System.ComponentModel.DataAnnotations;

namespace Aqua.ApiGateway.Configuration;

public sealed class GatewayOptions
{
    [Required] public required string JwtAuthority { get; init; }
    [Required] public required string JwtAudience { get; init; }
    // Default is true (fail-safe); internal-network deployments override to false via appsettings.json.
    public bool JwtRequireHttpsMetadata { get; init; } = true;

    [MinLength(1, ErrorMessage = "At least one downstream service must be configured.")]
    public IReadOnlyList<ServiceConfig> Services { get; init; } = Array.Empty<ServiceConfig>();
}
