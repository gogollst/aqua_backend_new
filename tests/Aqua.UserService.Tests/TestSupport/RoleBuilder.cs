using Aqua.UserService.Roles;

namespace Aqua.UserService.Tests.TestSupport;

public sealed class RoleBuilder
{
    private string _name = "Developer";
    private long _customerId = 1L;
    private PermissionBitset _perms = PermissionBitset.From(Permission.ReadRequirement);
    private bool _availableInProject = true;
    private bool _availableInCustomer = true;
    private bool _isDefault = false;

    public RoleBuilder WithName(string n)                 { _name = n; return this; }
    public RoleBuilder InCustomer(long id)                { _customerId = id; return this; }
    public RoleBuilder WithPerms(Permission p)            { _perms = PermissionBitset.From(p); return this; }
    public RoleBuilder AsDefault()                        { _isDefault = true; return this; }

    public Role Build() => new()
    {
        Name = _name,
        CustomerId = _customerId,
        Permissions = _perms,
        AvailableInProject = _availableInProject,
        AvailableInCustomer = _availableInCustomer,
        IsDefault = _isDefault,
    };
}
