namespace Aqua.UserService.Ldap;

public interface ILdapGroupRoleMappingRepository
{
    Task<LdapGroupRoleMapping?> FindByIdAsync(long id);
    Task<LdapGroupRoleMapping?> FindAsync(long customerId, string ldapGroupDn, long roleId);
    Task<IReadOnlyList<LdapGroupRoleMapping>> ListAsync(long customerId);
    Task InsertAsync(LdapGroupRoleMapping m);
    Task DeleteAsync(LdapGroupRoleMapping m);
}
