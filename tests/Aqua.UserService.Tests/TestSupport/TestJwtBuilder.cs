using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Aqua.UserService.Tests.TestSupport;

/// <summary>
/// Builds HS256-signed JWTs that the test <see cref="UserServiceWebApplicationFactory"/> accepts
/// as valid bearer tokens. The signing key is a deterministic constant — never use these tokens
/// outside the test pipeline.
/// </summary>
public sealed class TestJwtBuilder
{
    public const string Issuer  = "https://test.aqua-cloud.io";
    public const string Audience = "aqua-user-service";
    public const string SigningKey = "this-is-a-32-byte-test-key-for-tests-only!!!";

    private long _userId = 17L;
    private string _tenant = "acme";
    private long _tenantId = 1L;
    private long _perms = 0;
    private bool _serverAdmin = false;
    private string[] _roles = Array.Empty<string>();

    public TestJwtBuilder ForUser(long id)        { _userId = id; return this; }
    public TestJwtBuilder InTenant(string slug, long id = 1L) { _tenant = slug; _tenantId = id; return this; }
    public TestJwtBuilder WithPerms(long bits)    { _perms = bits; return this; }
    public TestJwtBuilder AsServerAdmin()         { _serverAdmin = true; return this; }
    public TestJwtBuilder WithRoles(params string[] roles) { _roles = roles; return this; }

    public string Build()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new("sub", _userId.ToString()),
            new(ClaimTypes.NameIdentifier, _userId.ToString()),
            new("tenant", _tenant),
            new("tenant_id", _tenantId.ToString()),
            new("perms", _perms.ToString()),
        };
        if (_serverAdmin) claims.Add(new Claim("serveradmin", "true"));
        foreach (var r in _roles) claims.Add(new Claim(ClaimTypes.Role, r));

        var token = new JwtSecurityToken(Issuer, Audience, claims,
            notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
