using Microsoft.OpenApi.Models;

namespace Aqua.ApiGateway.OpenApi;

public static class OpenApiAggregator
{
    public static OpenApiDocument Merge(IReadOnlyCollection<(string ServiceName, OpenApiDocument Document)> docs)
    {
        var merged = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "aqua API",
                Version = "v1",
                Description = "Aggregated OpenAPI spec across all aqua backend services.",
            },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, OpenApiSchema>(),
            },
        };

        var seenOperationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenSchemas      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (serviceName, doc) in docs)
        {
            foreach (var (path, item) in doc.Paths)
            {
                foreach (var op in item.Operations.Values)
                {
                    if (!string.IsNullOrEmpty(op.OperationId))
                    {
                        if (!seenOperationIds.Add(op.OperationId))
                            op.OperationId = $"{serviceName}.{op.OperationId}";
                        else
                            seenOperationIds.Add(op.OperationId);
                    }
                }

                merged.Paths[path] = item;
            }

            if (doc.Components?.Schemas is { } schemas)
            {
                foreach (var (name, schema) in schemas)
                {
                    var finalName = name;
                    if (!seenSchemas.Add(finalName))
                    {
                        finalName = $"{Capitalize(serviceName)}.{name}";
                        seenSchemas.Add(finalName);
                    }
                    merged.Components.Schemas[finalName] = schema;
                }
            }
        }

        return merged;
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
