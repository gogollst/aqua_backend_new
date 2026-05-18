using System.Security.Cryptography;
using Aqua.IdentityService.Configuration;
using Aqua.IdentityService.Tokens;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Aqua.IdentityService.Tests.Tokens;

public class JwtTokenIssuerTests : IDisposable
{
    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;
    private readonly RSA _rsa;
    private readonly IdentityOptions _options;

    public JwtTokenIssuerTests()
    {
        _rsa = RSA.Create(2048);
        _privateKeyPath = Path.GetTempFileName();
        _publicKeyPath = Path.GetTempFileName();
        File.WriteAllText(_privateKeyPath, _rsa.ExportPkcs8PrivateKeyPem());
        File.WriteAllText(_publicKeyPath, _rsa.ExportRSAPublicKeyPem());
        _options = new IdentityOptions
        {
            Issuer = "https://identity.aqua/test",
            Audience = "aqua-api",
            RsaPrivateKeyPath = _privateKeyPath,
            RsaPublicKeyPath = _publicKeyPath,
        };
    }

    [Fact]
    public void Issue_ProducesValidJwt()
    {
        var sut = new JwtTokenIssuer(Options.Create(_options));
        var pair = sut.Issue(42, "alice", "acme", new[] { "TestManager" });
        pair.AccessToken.Should().NotBeNullOrEmpty();
        pair.RefreshToken.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(pair.AccessToken);
        jwt.Issuer.Should().Be("https://identity.aqua/test");
        jwt.Audiences.Should().Contain("aqua-api");
        jwt.Claims.First(c => c.Type == "sub").Value.Should().Be("42");
        jwt.Claims.First(c => c.Type == "tenant").Value.Should().Be("acme");
        jwt.Claims.First(c => c.Type == "user_name").Value.Should().Be("alice");
        jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value).Should().Contain("TestManager");
    }

    [Fact]
    public void Issue_AccessTokenExpiresIn15Minutes_ByDefault()
    {
        var sut = new JwtTokenIssuer(Options.Create(_options));
        var before = DateTimeOffset.UtcNow;
        var pair = sut.Issue(1, "x", "t", Array.Empty<string>());
        pair.AccessTokenExpiresAt.Should().BeOnOrAfter(before.AddMinutes(15).AddSeconds(-5));
        pair.AccessTokenExpiresAt.Should().BeOnOrBefore(before.AddMinutes(15).AddSeconds(5));
    }

    [Fact]
    public void IssueRefreshToken_ReturnsBase64Url256Bits()
    {
        var sut = new JwtTokenIssuer(Options.Create(_options));
        var token = sut.IssueRefreshToken(1, "t");
        token.Should().NotBeNullOrEmpty();
        token.Length.Should().BeGreaterThan(40);
        token.Should().NotMatchRegex("[+/=]");
    }

    public void Dispose()
    {
        File.Delete(_privateKeyPath);
        File.Delete(_publicKeyPath);
        _rsa.Dispose();
    }
}
