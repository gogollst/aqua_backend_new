using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;

namespace Aqua.ApiGateway.OpenApi;

public static class SwaggerUiEndpoints
{
    public static IEndpointRouteBuilder MapAggregatedOpenApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/openapi.json", (OpenApiAggregationCache cache) =>
        {
            var (doc, _) = cache.Snapshot();
            if (doc is null)
                return Results.Json(new { type = "/problems/openapi-not-ready", title = "OpenAPI aggregation not ready yet", status = 503 },
                    statusCode: 503, contentType: "application/problem+json");

            var json = doc.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
            return Results.Content(json, "application/json");
        }).AllowAnonymous();

        return app;
    }
}
