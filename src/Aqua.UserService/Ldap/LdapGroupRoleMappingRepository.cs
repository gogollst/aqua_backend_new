using NHibernate;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Ldap;

public sealed class LdapGroupRoleMappingRepository : ILdapGroupRoleMappingRepository
{
    private readonly ISession _session;
    public LdapGroupRoleMappingRepository(ISession session) => _session = session;

    public async Task<LdapGroupRoleMapping?> FindByIdAsync(long id) =>
        await _session.GetAsync<LdapGroupRoleMapping>(id);

    public async Task<LdapGroupRoleMapping?> FindAsync(long customerId, string ldapGroupDn, long roleId) =>
        await _session.Query<LdapGroupRoleMapping>()
            .FirstOrDefaultAsync(m =>
                m.CustomerId == customerId &&
                m.LdapGroupDn == ldapGroupDn &&
                m.RoleId == roleId);

    public async Task<IReadOnlyList<LdapGroupRoleMapping>> ListAsync(long customerId) =>
        await _session.Query<LdapGroupRoleMapping>()
            .OrderBy(m => m.LdapGroupDn).ToListAsync();

    public Task InsertAsync(LdapGroupRoleMapping m) => _session.SaveAsync(m);
    public Task DeleteAsync(LdapGroupRoleMapping m) => _session.DeleteAsync(m);
}
