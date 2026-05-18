namespace Aqua.Contracts.OpenApi;

/// <summary>
/// Attribute marker for OpenAPI-tooling so error responses are documented with
/// Content-Type <c>application/problem+json</c> (per RFC 7807). Concrete OpenAPI tooling
/// (Swashbuckle / Microsoft.AspNetCore.OpenApi) consumes this attribute via a custom filter
/// in each service's startup.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class AquaProducesProblemAttribute : Attribute
{
    public int StatusCode { get; }
    public string ContentType { get; } = RestConventions.ProblemJsonContentType;

    public AquaProducesProblemAttribute(int statusCode) => StatusCode = statusCode;
}
