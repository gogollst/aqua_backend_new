using Aqua.Contracts.OpenApi;
using FluentAssertions;
using Xunit;

namespace Aqua.Contracts.Tests;

public class OpenApiTests
{
    [Fact]
    public void Conventions_DefaultJsonContentType_IsApplicationJson()
    {
        RestConventions.DefaultJsonContentType.Should().Be("application/json");
    }

    [Fact]
    public void Conventions_ProblemJsonContentType_IsApplicationProblemJson()
    {
        RestConventions.ProblemJsonContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public void Conventions_DefaultApiVersionPrefix_IsApiV1()
    {
        RestConventions.DefaultRoutePrefix.Should().Be("api/v1");
    }

    [Fact]
    public void ProducesProblemAttribute_HasContentTypeAndStatus()
    {
        var attr = new AquaProducesProblemAttribute(400);
        attr.StatusCode.Should().Be(400);
        attr.ContentType.Should().Be("application/problem+json");
    }
}
