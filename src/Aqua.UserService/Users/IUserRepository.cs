namespace Aqua.UserService.Users;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(long id);
    Task<User?> FindByUsernameAsync(string username);
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByLdapDnAsync(long customerId, string ldapDn);
    Task<IReadOnlyList<User>> ListAsync(long customerId, int skip, int take, string? search);
    Task<long> CountAsync(long customerId, string? search);
    Task InsertAsync(User user);
    Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<long> ids);
}
