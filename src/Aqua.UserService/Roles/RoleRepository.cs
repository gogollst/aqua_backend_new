using NHibernate;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Roles;

public sealed class RoleRepository : IRoleRepository
{
    private readonly ISession _session;
    public RoleRepository(ISession session) => _session = session;

    public async Task<Role?> FindByIdAsync(long id) =>
        await _session.GetAsync<Role>(id);

    public async Task<Role?> FindByNameAsync(long customerId, string name) =>
        await _session.Query<Role>().FirstOrDefaultAsync(r => r.CustomerId == customerId && r.Name == name);

    public async Task<IReadOnlyList<Role>> ListAsync(long customerId) =>
        await _session.Query<Role>().OrderBy(r => r.Name).ToListAsync();

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IReadOnlyCollection<long> ids)
    {
        if (ids.Count == 0) return Array.Empty<Role>();
        return await _session.Query<Role>().Where(r => ids.Contains(r.Id)).ToListAsync();
    }

    public Task InsertAsync(Role role) => _session.SaveAsync(role);
    public Task DeleteAsync(Role role) => _session.DeleteAsync(role);
}
