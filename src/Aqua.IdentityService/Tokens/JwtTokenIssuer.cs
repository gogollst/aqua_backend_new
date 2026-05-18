using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Aqua.IdentityService.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aqua.IdentityService.Tokens;

public sealed class JwtTokenIssuer : ITokenIssuer, IDisposable
{
    private readonly IdentityOptions _options;
    private readonly RSA _signingKey;
    private readonly SigningCredentials _credentials;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenIssuer(IOptions<IdentityOptions> options)
    {
        _options = options.Value;
        _signingKey = RsaKeyLoader.LoadFromPem(_options.RsaPrivateKeyPath);
        var securityKey = new RsaSecurityKey(_signingKey) { KeyId = _options.SigningKeyId };
        _credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
        _handler.MapInboundClaims = false;
    }

    public TokenPair Issue(int userId, string userName, string tenantId, IReadOnlyList<string> roles)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(_options.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("user_name", userName),
            new("tenant", tenantId),
            new("jti", Guid.NewGuid().ToString()),
        };
        foreach (var role in roles) claims.Add(new("roles", role));

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: _credentials);

        return new TokenPair(
            AccessToken: _handler.WriteToken(jwt),
            RefreshToken: IssueRefreshToken(userId, tenantId),
            AccessTokenExpiresAt: expires);
    }

    public string IssueRefreshToken(int userId, string tenantId)
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncoder.Encode(bytes.ToArray());
    }

    public void Dispose() => _signingKey.Dispose();
}
