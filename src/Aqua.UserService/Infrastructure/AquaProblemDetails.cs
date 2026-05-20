using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Infrastructure;

public sealed class AquaProblemDetails : ProblemDetails
{
    public string? CorrelationId { get; init; }
    public string ErrorCode { get; init; } = "server.unhandled";
    public IReadOnlyList<FieldError>? Errors { get; init; }
}

public sealed record FieldError(string Field, string Message);
