namespace Aqua.UserService.Tenants;

public class Customer
{
    public virtual long Id { get; set; }
    public virtual string Slug { get; set; } = "";
    public virtual string DisplayName { get; set; } = "";
    public virtual string? PrimaryDomain { get; set; }
    public virtual TenantAuthMode AuthMode { get; set; } = TenantAuthMode.Local;
    public virtual string? AuthConfigJson { get; set; }
    public virtual long Version { get; set; }
}
