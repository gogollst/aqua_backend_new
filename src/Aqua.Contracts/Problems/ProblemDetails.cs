using System.Text.Json.Serialization;

namespace Aqua.Contracts.Problems;

/// <summary>
/// RFC 7807 problem-detail response with aqua-specific traceId extension.
/// </summary>
public sealed class AquaProblemDetails
{
    [JsonPropertyName("type")]      public string? Type     { get; init; }
    [JsonPropertyName("title")]     public string? Title    { get; init; }
    [JsonPropertyName("status")]    public int?    Status   { get; init; }
    [JsonPropertyName("detail")]    public string? Detail   { get; init; }
    [JsonPropertyName("instance")]  public string? Instance { get; init; }
    [JsonPropertyName("traceId")]   public string? TraceId  { get; init; }

    /// <summary>
    /// Additional problem-specific fields serialized at the top level.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> Extensions { get; } = new Dictionary<string, object?>();
}
