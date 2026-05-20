using NHibernate;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Users;

public sealed class UserRepository : IUserRepository
{
    private readonly ISession _session;
    public UserRepository(ISession session) => _session = session;

    public async Task<User?> FindByIdAsync(long id) =>
        await _session.GetAsync<User>(id);

    public async Task<User?> FindByUsernameAsync(string username) =>
        await _session.Query<User>().FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User?> FindByEmailAsync(string email) =>
        await _session.Query<User>().FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> FindByLdapDnAsync(long customerId, string ldapDn) =>
        await _session.Query<User>()
            .FirstOrDefaultAsync(u => u.CustomerIdHint == customerId && u.LdapDn == ldapDn);

    public async Task<IReadOnlyList<User>> ListAsync(long customerId, int skip, int take, string? search)
    {
        var q = _session.Query<User>().Where(u => !u.Deleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
        }
        return await q.OrderBy(u => u.Username).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<long> CountAsync(long customerId, string? search)
    {
        var q = _session.Query<User>().Where(u => !u.Deleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
        }
        return await q.LongCountAsync();
    }

    public async Task InsertAsync(User user)
    {
        await _session.SaveAsync(user);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<long> ids)
    {
        if (ids.Count == 0) return Array.Empty<User>();
        return await _session.Query<User>().Where(u => ids.Contains(u.Id)).ToListAsync();
    }
}
