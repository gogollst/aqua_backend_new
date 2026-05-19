using Microsoft.OpenApi.Models;

namespace Aqua.ApiGateway.OpenApi;

/// <summary>Thread-safe holder for the most recently successfully-aggregated OpenAPI document.</summary>
public sealed class OpenApiAggregationCache
{
    private readonly Lock _gate = new();
    private OpenApiDocument? _document;
    private DateTimeOffset? _lastSuccessUtc;

    public (OpenApiDocument? Document, DateTimeOffset? LastSuccessUtc) Snapshot()
    {
        lock (_gate)
        {
            return (_document, _lastSuccessUtc);
        }
    }

    public void Set(OpenApiDocument document)
    {
        lock (_gate)
        {
            _document = document;
            _lastSuccessUtc = DateTimeOffset.UtcNow;
        }
    }
}
