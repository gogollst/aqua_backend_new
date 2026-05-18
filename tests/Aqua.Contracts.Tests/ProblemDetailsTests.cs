using Aqua.Contracts.Problems;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Aqua.Contracts.Tests;

public class ProblemDetailsTests
{
    [Fact]
    public void Construction_FullProblem_SerializesToRfc7807Json()
    {
        var problem = new AquaProblemDetails
        {
            Type = ProblemTypes.ValidationError,
            Title = "Validation failed",
            Status = 400,
            Detail = "Field 'email' is required.",
            Instance = "/api/v1/users/-",
            TraceId = "trace-abc-123",
        };
        problem.Extensions["field"] = "email";

        var json = JsonSerializer.Serialize(problem);

        json.Should().Contain("\"type\":\"https://aqua-cloud.io/problems/validation-error\"");
        json.Should().Contain("\"title\":\"Validation failed\"");
        json.Should().Contain("\"status\":400");
        json.Should().Contain("\"traceId\":\"trace-abc-123\"");
        json.Should().Contain("\"field\":\"email\"");
    }

    [Fact]
    public void ProblemTypes_AreStableUris()
    {
        ProblemTypes.ValidationError.Should().StartWith("https://aqua-cloud.io/problems/");
        ProblemTypes.NotFound.Should().StartWith("https://aqua-cloud.io/problems/");
        ProblemTypes.Unauthorized.Should().StartWith("https://aqua-cloud.io/problems/");
        ProblemTypes.Forbidden.Should().StartWith("https://aqua-cloud.io/problems/");
        ProblemTypes.Conflict.Should().StartWith("https://aqua-cloud.io/problems/");
    }
}
