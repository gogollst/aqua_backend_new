using Aqua.UserService.Domain;
using Aqua.UserService.Users.Dto;

namespace Aqua.UserService.Users;

public interface IUserManager
{
    Task<UserDto> CreateAsync(CreateUserRequest req, long customerId);
    Task<UserDto> GetAsync(long id);
    Task<UserDto> PatchAsync(long id, PatchUserRequest req, long customerId);
    Task SoftDeleteAsync(long id, long customerId);
    Task<IReadOnlyList<UserDto>> ListAsync(long customerId, int skip, int take, string? search);
    Task<IReadOnlyList<UserDto>> GetByIdsAsync(IReadOnlyCollection<long> ids);
}

public sealed class UserManager : IUserManager
{
    private readonly IUserRepository _repo;

    public UserManager(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest req, long customerId)
    {
        if (await _repo.FindByUsernameAsync(req.Username) is not null)
            throw new ConflictException("user.username-taken", $"Username '{req.Username}' is already taken.");
        if (await _repo.FindByEmailAsync(req.Email) is not null)
            throw new ConflictException("user.email-taken", $"Email '{req.Email}' is already taken.");

        var user = new User
        {
            Username = req.Username,
            Email    = req.Email,
            FirstName = req.FirstName,
            Surname  = req.Surname,
            Phone    = req.Phone,
            Position = req.Position,
            Status   = UserStatus.Active,
            CustomerIdHint = customerId,
        };
        await _repo.InsertAsync(user);
        return ToDto(user);
    }

    public async Task<UserDto> GetAsync(long id)
    {
        var u = await _repo.FindByIdAsync(id)
            ?? throw NotFoundException.ForUser(id);
        return ToDto(u);
    }

    public async Task<UserDto> PatchAsync(long id, PatchUserRequest req, long customerId)
    {
        var u = await _repo.FindByIdAsync(id) ?? throw NotFoundException.ForUser(id);
        if (u.Version != req.Version)
            throw new StaleVersionException(u.Version, $"User version {req.Version} is stale (current = {u.Version}).");

        if (req.FirstName is not null) u.FirstName = req.FirstName;
        if (req.Surname   is not null) u.Surname   = req.Surname;
        if (req.Email     is not null) u.Email     = req.Email;
        if (req.Phone     is not null) u.Phone     = req.Phone;
        if (req.Position  is not null) u.Position  = req.Position;
        return ToDto(u);
    }

    public async Task SoftDeleteAsync(long id, long customerId)
    {
        var u = await _repo.FindByIdAsync(id) ?? throw NotFoundException.ForUser(id);
        u.Deleted = true;
        u.Status = UserStatus.Disabled;
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(long customerId, int skip, int take, string? search)
    {
        var users = await _repo.ListAsync(customerId, skip, take, search);
        return users.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<UserDto>> GetByIdsAsync(IReadOnlyCollection<long> ids)
    {
        var users = await _repo.GetByIdsAsync(ids);
        return users.Select(ToDto).ToList();
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.Username, u.FirstName, u.Surname, u.Email,
        u.Phone, u.Position, u.Status, u.ServerAdmin, u.Deleted, u.LdapDn, u.Version);
}
