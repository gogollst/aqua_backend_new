namespace Aqua.Contracts.Problems;

/// <summary>
/// Stable "type" URIs used in RFC 7807 ProblemDetails responses.
/// </summary>
public static class ProblemTypes
{
    private const string BaseUri = "https://aqua-cloud.io/problems/";

    public const string ValidationError = BaseUri + "validation-error";
    public const string NotFound        = BaseUri + "not-found";
    public const string Unauthorized    = BaseUri + "unauthorized";
    public const string Forbidden       = BaseUri + "forbidden";
    public const string Conflict        = BaseUri + "conflict";
    public const string TooManyRequests = BaseUri + "too-many-requests";
    public const string InternalError   = BaseUri + "internal-error";
}
