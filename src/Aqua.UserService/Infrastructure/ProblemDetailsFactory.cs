using Microsoft.AspNetCore.Http;

namespace Aqua.UserService.Infrastructure;

public sealed class ProblemDetailsFactory
{
    public AquaProblemDetails Build(
        int status,
        string errorCode,
        string title,
        string? detail,
        string? correlationId,
        IReadOnlyList<FieldError>? errors = null) =>
        new()
        {
            Type    = $"https://aqua-cloud.io/problems/{errorCode}",
            Title   = title,
            Status  = status,
            Detail  = detail,
            ErrorCode = errorCode,
            CorrelationId = correlationId,
            Errors  = errors,
        };

    public string? GetCorrelationId(HttpContext http) =>
        http.Request.Headers.TryGetValue("X-Correlation-Id", out var v)
            ? v.ToString()
            : http.TraceIdentifier;
}
