using Aqua.IdentityService.Configuration;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Aqua.IdentityService.Tests.Configuration;

public class OptionsValidationTests
{
    [Fact]
    public void IdentityOptions_DefaultLifetimes_AreSane()
    {
        var opts = new IdentityOptions
        {
            Issuer = "https://identity.aqua/test",
            Audience = "aqua-api",
            RsaPrivateKeyPath = "/dev/null",
            RsaPublicKeyPath = "/dev/null",
        };
        opts.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(15));
        opts.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(14));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IdentityOptions_MissingIssuer_FailsValidation(string? issuer)
    {
        var opts = new IdentityOptions { Issuer = issuer!, Audience = "x", RsaPrivateKeyPath = "/a", RsaPublicKeyPath = "/b" };
        var results = new List<ValidationResult>();
        var ok = Validator.TryValidateObject(opts, new ValidationContext(opts), results, validateAllProperties: true);
        ok.Should().BeFalse();
    }

    [Fact]
    public void AuthenticationOptions_Default_IsDatabaseOnly()
    {
        var opts = new AuthenticationOptions();
        opts.Mode.Should().Be(AuthenticationMode.DatabaseOnly);
    }
}
