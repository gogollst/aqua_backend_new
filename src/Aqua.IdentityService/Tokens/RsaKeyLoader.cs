using System.Security.Cryptography;

namespace Aqua.IdentityService.Tokens;

public static class RsaKeyLoader
{
    public static RSA LoadFromPem(string path)
    {
        var pem = File.ReadAllText(path);
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return rsa;
    }
}
