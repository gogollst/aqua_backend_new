using System.Security.Cryptography;
using Aqua.IdentityService.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aqua.IdentityService.Tokens;

public sealed class JwksProvider : IDisposable
{
    private readonly RSA _publicKey;
    private readonly string _keyId;

    public JwksProvider(IOptions<IdentityOptions> options)
    {
        var opts = options.Value;
        _publicKey = RsaKeyLoader.LoadFromPem(opts.RsaPublicKeyPath);
        _keyId = opts.SigningKeyId;
    }

    public object GetJwks()
    {
        var parameters = _publicKey.ExportParameters(includePrivateParameters: false);
        return new
        {
            keys = new object[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    kid = _keyId,
                    alg = "RS256",
                    n = Base64UrlEncoder.Encode(parameters.Modulus!),
                    e = Base64UrlEncoder.Encode(parameters.Exponent!),
                }
            }
        };
    }

    public void Dispose() => _publicKey.Dispose();
}
