using System.Text;
using Aqua.ApiGateway.OpenApi;
using FluentAssertions;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Aqua.ApiGateway.Tests.OpenApi;

public class OpenApiAggregatorTests
{
    [Fact]
    public void Merges_paths_from_multiple_services()
    {
        var identity = LoadDoc(@"openapi: 3.0.0
info: { title: Identity, version: '1' }
paths:
  /api/v1/auth/token: { post: { operationId: PostToken, responses: { '200': { description: OK } } } }");

        var users = LoadDoc(@"openapi: 3.0.0
info: { title: Users, version: '1' }
paths:
  /api/v1/users/{id}: { get: { operationId: GetUser, responses: { '200': { description: OK } } } }");

        var merged = OpenApiAggregator.Merge(new (string Name, OpenApiDocument Doc)[]
        {
            ("identity", identity),
            ("users",    users),
        });

        merged.Paths.Keys.Should().BeEquivalentTo("/api/v1/auth/token", "/api/v1/users/{id}");
        merged.Info.Title.Should().Be("aqua API");
    }

    [Fact]
    public void Prefixes_operationId_on_collision()
    {
        var a = LoadDoc(@"openapi: 3.0.0
info: { title: A, version: '1' }
paths:
  /a: { get: { operationId: GetThing, responses: { '200': { description: OK } } } }");

        var b = LoadDoc(@"openapi: 3.0.0
info: { title: B, version: '1' }
paths:
  /b: { get: { operationId: GetThing, responses: { '200': { description: OK } } } }");

        var merged = OpenApiAggregator.Merge(new (string Name, OpenApiDocument Doc)[]
        {
            ("first",  a),
            ("second", b),
        });

        merged.Paths["/a"].Operations[OperationType.Get].OperationId.Should().Be("GetThing");
        merged.Paths["/b"].Operations[OperationType.Get].OperationId.Should().Be("second.GetThing");
    }

    [Fact]
    public void Returns_empty_document_when_no_services()
    {
        var merged = OpenApiAggregator.Merge(Array.Empty<(string, OpenApiDocument)>());

        merged.Paths.Should().BeEmpty();
        merged.Info.Should().NotBeNull();
    }

    private static OpenApiDocument LoadDoc(string yaml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(yaml));
        var reader = new Microsoft.OpenApi.Readers.OpenApiStreamReader();
        var doc = reader.Read(stream, out var diagnostic);
        diagnostic.Errors.Should().BeEmpty();
        return doc;
    }
}
