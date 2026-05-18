using System.ComponentModel.DataAnnotations;
using Aqua.ApiGateway.Configuration;
using FluentAssertions;
using Xunit;

namespace Aqua.ApiGateway.Tests.Configuration;

public class OptionsValidationTests
{
    [Fact]
    public void GatewayOptions_RequiresAtLeastOneService()
    {
        var opts = new GatewayOptions { JwtAuthority = "http://identity:8080", JwtAudience = "aqua-api" };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(opts, new ValidationContext(opts), results, true).Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(GatewayOptions.Services)));
    }

    [Fact]
    public void TenantResolutionOptions_DefaultMode_IsDefault()
    {
        var opts = new TenantResolutionOptions { DefaultTenant = "acme" };
        opts.Mode.Should().Be(TenantResolutionMode.Default);
    }

    [Fact]
    public void TenantResolutionOptions_SubdomainMode_RequiresPattern()
    {
        var opts = new TenantResolutionOptions { Mode = TenantResolutionMode.Subdomain };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(opts, new ValidationContext(opts), results, true).Should().BeFalse();
    }

    [Fact]
    public void RateLimitOptions_HasSensibleDefaults()
    {
        var opts = new RateLimitOptions();
        opts.PerIp.PermitLimit.Should().Be(100);
        opts.PerTenant.PermitLimit.Should().Be(1000);
        opts.PerUser.PermitLimit.Should().Be(600);
    }

    [Fact]
    public void ResilienceOptions_HasSensibleDefaults()
    {
        var opts = new ResilienceOptions();
        opts.RetryAttempts.Should().Be(2);
        opts.CircuitBreakerFailureRatio.Should().Be(0.5);
        opts.RequestTimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void ResilienceOptions_RejectsNegativeRetryAttempts()
    {
        var opts = new ResilienceOptions { RetryAttempts = -1 };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(opts, new ValidationContext(opts), results, true).Should().BeFalse();
    }

    [Fact]
    public void ResilienceOptions_RejectsFailureRatioAboveOne()
    {
        var opts = new ResilienceOptions { CircuitBreakerFailureRatio = 1.5 };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(opts, new ValidationContext(opts), results, true).Should().BeFalse();
    }
}
