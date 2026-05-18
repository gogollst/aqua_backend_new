using System.ComponentModel.DataAnnotations;

namespace Aqua.IdentityService.Configuration;

public sealed class IdentityOptions
{
    [Required, MinLength(1)] public required string Issuer { get; init; }
    [Required, MinLength(1)] public required string Audience { get; init; }

    /// <summary>Path to RSA private key in PKCS#8 PEM format. Used to sign JWTs.</summary>
    [Required] public required string RsaPrivateKeyPath { get; init; }

    /// <summary>Path to RSA public key in PKCS#8 PEM format. Published via JWKS endpoint.</summary>
    [Required] public required string RsaPublicKeyPath { get; init; }

    /// <summary>Key-id claim ("kid") for the signing key. Lets us rotate keys later.</summary>
    public string SigningKeyId { get; init; } = "aqua-identity-1";

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(14);
    public TimeSpan ClockSkewTolerance { get; init; } = TimeSpan.FromSeconds(30);
}
