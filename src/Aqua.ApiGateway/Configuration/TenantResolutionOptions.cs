using System.ComponentModel.DataAnnotations;

namespace Aqua.ApiGateway.Configuration;

public enum TenantResolutionMode
{
    Default,
    Subdomain,
}

public sealed class TenantResolutionOptions : IValidatableObject
{
    public TenantResolutionMode Mode { get; init; } = TenantResolutionMode.Default;
    public string? SubdomainPattern { get; init; }
    public IReadOnlyList<string> ReservedSubdomains { get; init; } = new[] { "www", "api", "admin", "status" };
    public string? DefaultTenant { get; init; }
    public string HeaderName { get; init; } = "X-Aqua-Tenant";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Mode == TenantResolutionMode.Subdomain && string.IsNullOrWhiteSpace(SubdomainPattern))
            yield return new ValidationResult("SubdomainPattern is required when Mode=Subdomain.", new[] { nameof(SubdomainPattern) });
        if (Mode == TenantResolutionMode.Default && string.IsNullOrWhiteSpace(DefaultTenant))
            yield return new ValidationResult("DefaultTenant is required when Mode=Default.", new[] { nameof(DefaultTenant) });
    }
}
