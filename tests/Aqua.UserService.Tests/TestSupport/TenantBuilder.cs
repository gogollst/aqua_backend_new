using Aqua.UserService.Tenants;

namespace Aqua.UserService.Tests.TestSupport;

public sealed class TenantBuilder
{
    private string _slug = "acme";
    private string _displayName = "Acme";
    private TenantAuthMode _authMode = TenantAuthMode.Local;

    public TenantBuilder WithSlug(string s) { _slug = s; return this; }
    public TenantBuilder WithAuthMode(TenantAuthMode m) { _authMode = m; return this; }

    public Customer Build() => new()
    {
        Slug = _slug,
        DisplayName = _displayName,
        AuthMode = _authMode,
    };
}
