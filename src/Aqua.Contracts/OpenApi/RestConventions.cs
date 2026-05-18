namespace Aqua.Contracts.OpenApi;

/// <summary>
/// Aqua-wide REST/OpenAPI conventions referenced by every service.
/// Keeps URL prefixes, content types, and header names consistent across services.
/// </summary>
public static class RestConventions
{
    public const string DefaultJsonContentType    = "application/json";
    public const string ProblemJsonContentType    = "application/problem+json";
    public const string DefaultRoutePrefix        = "api/v1";

    public const string TenantHeader              = "X-Aqua-Tenant";
    public const string OriginalUserHeader        = "X-Aqua-Original-User";
    public const string CorrelationHeader         = "X-Correlation-Id";
    public const string IdempotencyKeyHeader      = "Idempotency-Key";

    public const string PaginationTotalCountHeader = "X-Total-Count";
    public const string DefaultSortQueryParam     = "sort";
    public const string DefaultFilterQueryParam   = "$filter";
}
