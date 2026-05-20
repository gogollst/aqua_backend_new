using Aqua.UserService.Users;

namespace Aqua.UserService.Tests.TestSupport;

public sealed class UserBuilder
{
    private string _username = "alice";
    private string _firstName = "Alice";
    private string _surname  = "Anderson";
    private string _email    = "alice@example.com";
    private UserStatus _status = UserStatus.Active;
    private bool _serverAdmin = false;
    private string? _ldapDn = null;
    private long _customerId = 1L;

    public UserBuilder WithUsername(string u)     { _username = u; return this; }
    public UserBuilder WithEmail(string e)        { _email = e; return this; }
    public UserBuilder WithStatus(UserStatus s)   { _status = s; return this; }
    public UserBuilder WithServerAdmin(bool sa=true){ _serverAdmin = sa; return this; }
    public UserBuilder WithLdapDn(string dn)      { _ldapDn = dn; return this; }
    public UserBuilder InCustomer(long id)        { _customerId = id; return this; }

    public User Build() => new()
    {
        Username = _username,
        FirstName = _firstName,
        Surname  = _surname,
        Email    = _email,
        Status   = _status,
        ServerAdmin = _serverAdmin,
        LdapDn   = _ldapDn,
        CustomerIdHint = _customerId,
    };
}
