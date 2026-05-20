namespace Aqua.UserService.Tenants;

public interface ICustomerUserAssignmentRepository
{
    Task<IReadOnlyList<long>> GetRoleIdsAsync(long customerId, long userId);
    Task AssignRolesAsync(long customerId, long userId, IReadOnlyCollection<long> roleIds);
    Task<IReadOnlyList<CustomerUserAssignment>> GetByUserIdAsync(long customerId, long userId);
}
