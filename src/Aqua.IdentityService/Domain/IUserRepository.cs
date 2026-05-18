namespace Aqua.IdentityService.Domain;

public interface IUserRepository
{
    Task<AquaUser?> FindByUserNameAsync(string userName, CancellationToken ct = default);
    Task<AquaUserPassword?> GetPasswordForAsync(int userId, CancellationToken ct = default);
    Task<AquaUser?> GetByIdAsync(int userId, CancellationToken ct = default);
    Task IncrementFailedLoginAsync(int userId, CancellationToken ct = default);
    Task ResetFailedLoginAsync(int userId, CancellationToken ct = default);
}
