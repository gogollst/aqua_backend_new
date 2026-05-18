using System.ComponentModel.DataAnnotations;

namespace Aqua.ApiGateway.Configuration;

public sealed class ServiceConfig
{
    [Required] public required string Name { get; init; }
    [Required, Url] public required string BaseUrl { get; init; }
    [Required] public required string PathPrefix { get; init; }
    public IReadOnlyList<string> AnonymousPaths { get; init; } = Array.Empty<string>();
}
