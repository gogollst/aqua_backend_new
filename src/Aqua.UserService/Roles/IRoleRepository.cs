namespace Aqua.UserService.Roles;

public interface IRoleRepository
{
    Task<Role?> FindByIdAsync(long id);
    Task<Role?> FindByNameAsync(long customerId, string name);
    Task<IReadOnlyList<Role>> ListAsync(long customerId);
    Task<IReadOnlyList<Role>> GetByIdsAsync(IReadOnlyCollection<long> ids);
    Task InsertAsync(Role role);
    Task DeleteAsync(Role role);
}
