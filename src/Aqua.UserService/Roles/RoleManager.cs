using Aqua.UserService.Domain;
using Aqua.UserService.Events;
using Aqua.UserService.Roles.Dto;

namespace Aqua.UserService.Roles;

public interface IRoleManager
{
    Task<(RoleDto Role, RoleMutationWarnings Warnings)> CreateAsync(CreateRoleRequest req, long customerId);
    Task<RoleDto> GetAsync(long id);
    Task<(RoleDto Role, RoleMutationWarnings Warnings)> PatchAsync(long id, PatchRoleRequest req, long customerId);
    Task DeleteAsync(long id, long customerId);
    Task<IReadOnlyList<RoleDto>> ListAsync(long customerId);
    Task<IReadOnlyList<RoleDto>> GetByIdsAsync(IReadOnlyCollection<long> ids);
}

public sealed class RoleManager : IRoleManager
{
    private readonly IRoleRepository _repo;
    private readonly IUserEventPublisher _publisher;

    public RoleManager(IRoleRepository repo, IUserEventPublisher publisher)
    {
        _repo = repo;
        _publisher = publisher;
    }

    public async Task<(RoleDto, RoleMutationWarnings)> CreateAsync(CreateRoleRequest req, long customerId)
    {
        if (await _repo.FindByNameAsync(customerId, req.Name) is not null)
            throw new ConflictException("role.name-taken", $"Role '{req.Name}' already exists in this tenant.");

        var inputBitset = PermissionBitset.From((Permission)req.Permissions);
        var (closure, added) = inputBitset.EnforceDependencies();
        var role = new Role
        {
            Name = req.Name,
            Description = req.Description,
            CustomerId = customerId,
            AvailableInProject = req.AvailableInProject,
            AvailableInCustomer = req.AvailableInCustomer,
            IsDefault = req.IsDefault,
            Permissions = closure,
        };
        await _repo.InsertAsync(role);
        await _publisher.PublishAsync(customerId, "role.created",
            new RoleCreated(role.Id, customerId));
        return (ToDto(role), new RoleMutationWarnings(added.Select(p => p.ToString()).ToList()));
    }

    public async Task<RoleDto> GetAsync(long id)
    {
        var r = await _repo.FindByIdAsync(id) ?? throw NotFoundException.ForRole(id);
        return ToDto(r);
    }

    public async Task<(RoleDto, RoleMutationWarnings)> PatchAsync(long id, PatchRoleRequest req, long customerId)
    {
        var r = await _repo.FindByIdAsync(id) ?? throw NotFoundException.ForRole(id);
        if (r.Version != req.Version)
            throw new StaleVersionException(r.Version, $"Role version mismatch (request {req.Version}, current {r.Version}).");

        if (req.Name is not null)              r.Name = req.Name;
        if (req.Description is not null)       r.Description = req.Description;
        if (req.AvailableInProject is { } aip) r.AvailableInProject = aip;
        if (req.AvailableInCustomer is { } aic) r.AvailableInCustomer = aic;
        if (req.IsDefault is { } isd)          r.IsDefault = isd;

        var addedNames = new List<string>();
        if (req.Permissions is { } permsLong)
        {
            var input = PermissionBitset.From((Permission)permsLong);
            var (closure, added) = input.EnforceDependencies();
            r.Permissions = closure;
            addedNames = added.Select(p => p.ToString()).ToList();
        }
        await _publisher.PublishAsync(customerId, "role.updated",
            new RoleUpdated(r.Id, customerId));
        return (ToDto(r), new RoleMutationWarnings(addedNames));
    }

    public async Task DeleteAsync(long id, long customerId)
    {
        var r = await _repo.FindByIdAsync(id) ?? throw NotFoundException.ForRole(id);
        await _repo.DeleteAsync(r);
        await _publisher.PublishAsync(customerId, "role.deleted",
            new RoleDeleted(r.Id, customerId));
    }

    public async Task<IReadOnlyList<RoleDto>> ListAsync(long customerId)
    {
        var rs = await _repo.ListAsync(customerId);
        return rs.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<RoleDto>> GetByIdsAsync(IReadOnlyCollection<long> ids)
    {
        var rs = await _repo.GetByIdsAsync(ids);
        return rs.Select(ToDto).ToList();
    }

    private static RoleDto ToDto(Role r) => new(
        r.Id, r.Name, r.Description, r.CustomerId,
        r.AvailableInProject, r.AvailableInCustomer, r.IsDefault,
        (long)r.Permissions.Flags, r.PermVersion, r.Version);
}
