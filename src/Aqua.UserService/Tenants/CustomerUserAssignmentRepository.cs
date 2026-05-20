using NHibernate;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Tenants;

public sealed class CustomerUserAssignmentRepository : ICustomerUserAssignmentRepository
{
    private readonly ISession _session;
    public CustomerUserAssignmentRepository(ISession session) => _session = session;

    public async Task<IReadOnlyList<long>> GetRoleIdsAsync(long customerId, long userId) =>
        await _session.Query<CustomerUserAssignment>()
            .Where(a => a.CustomerId == customerId && a.UserId == userId)
            .Select(a => a.RoleId)
            .ToListAsync();

    public async Task AssignRolesAsync(long customerId, long userId, IReadOnlyCollection<long> roleIds)
    {
        var existing = await _session.Query<CustomerUserAssignment>()
            .Where(a => a.CustomerId == customerId && a.UserId == userId)
            .ToListAsync();

        var current = existing.ToDictionary(a => a.RoleId);
        var target  = new HashSet<long>(roleIds);

        // delete removed
        foreach (var (roleId, row) in current)
        {
            if (!target.Contains(roleId))
                await _session.DeleteAsync(row);
        }

        // insert new
        foreach (var roleId in target)
        {
            if (!current.ContainsKey(roleId))
            {
                await _session.SaveAsync(new CustomerUserAssignment
                {
                    CustomerId = customerId,
                    UserId = userId,
                    RoleId = roleId,
                });
            }
        }
    }

    public async Task<IReadOnlyList<CustomerUserAssignment>> GetByUserIdAsync(long customerId, long userId) =>
        await _session.Query<CustomerUserAssignment>()
            .Where(a => a.CustomerId == customerId && a.UserId == userId)
            .ToListAsync();
}
