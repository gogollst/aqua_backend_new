using Aqua.Data.Sessions;
using NHibernate.Linq;

namespace Aqua.IdentityService.Domain;

public sealed class UserRepository : IUserRepository
{
    private readonly ISessionScope _scope;
    public UserRepository(ISessionScope scope) => _scope = scope;

    public Task<AquaUser?> FindByUserNameAsync(string userName, CancellationToken ct = default) =>
        _scope.Session.Query<AquaUser>().FirstOrDefaultAsync(u => u.UserName == userName, ct)!;

    public Task<AquaUserPassword?> GetPasswordForAsync(int userId, CancellationToken ct = default) =>
        _scope.Session.GetAsync<AquaUserPassword>(userId, ct)!;

    public Task<AquaUser?> GetByIdAsync(int userId, CancellationToken ct = default) =>
        _scope.Session.GetAsync<AquaUser>(userId, ct)!;

    public async Task IncrementFailedLoginAsync(int userId, CancellationToken ct = default)
    {
        var pwd = await _scope.Session.GetAsync<AquaUserPassword>(userId, ct);
        if (pwd is null) return;
        pwd.FailedLoginCount++;
        await _scope.Session.UpdateAsync(pwd, ct);
    }

    public async Task ResetFailedLoginAsync(int userId, CancellationToken ct = default)
    {
        var pwd = await _scope.Session.GetAsync<AquaUserPassword>(userId, ct);
        if (pwd is null) return;
        pwd.FailedLoginCount = 0;
        pwd.LockedUntil = null;
        await _scope.Session.UpdateAsync(pwd, ct);
    }
}
