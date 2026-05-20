namespace Aqua.UserService.Tenants;

public enum TenantAuthMode : long
{
    Local = 0,
    Ldap  = 1,
    Saml  = 2,
}
