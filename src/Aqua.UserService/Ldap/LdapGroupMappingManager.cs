using Aqua.UserService.Domain;
using Aqua.UserService.Ldap.Dto;

namespace Aqua.UserService.Ldap;

public interface ILdapGroupMappingManager
{
    Task<LdapGroupMappingDto> CreateAsync(CreateLdapMappingRequest req, long customerId);
    Task<IReadOnlyList<LdapGroupMappingDto>> ListAsync(long customerId);
    Task DeleteAsync(long id, long customerId);
}

public sealed class LdapGroupMappingManager : ILdapGroupMappingManager
{
    private readonly ILdapGroupRoleMappingRepository _repo;
    public LdapGroupMappingManager(ILdapGroupRoleMappingRepository repo) => _repo = repo;

    public async Task<LdapGroupMappingDto> CreateAsync(CreateLdapMappingRequest req, long customerId)
    {
        if (await _repo.FindAsync(customerId, req.LdapGroupDn, req.RoleId) is not null)
            throw new ConflictException("ldap-mapping.duplicate",
                $"Mapping for group '{req.LdapGroupDn}' -> role {req.RoleId} already exists.");

        var m = new LdapGroupRoleMapping
        {
            CustomerId = customerId,
            LdapGroupDn = req.LdapGroupDn,
            RoleId = req.RoleId,
            CreatedAt = DateTime.UtcNow,
        };
        await _repo.InsertAsync(m);
        return ToDto(m);
    }

    public async Task<IReadOnlyList<LdapGroupMappingDto>> ListAsync(long customerId)
    {
        var list = await _repo.ListAsync(customerId);
        return list.Select(ToDto).ToList();
    }

    public async Task DeleteAsync(long id, long customerId)
    {
        var m = await _repo.FindByIdAsync(id)
            ?? throw new NotFoundException("ldap-mapping.not-found", $"Mapping {id} not found.");
        await _repo.DeleteAsync(m);
    }

    private static LdapGroupMappingDto ToDto(LdapGroupRoleMapping m) =>
        new(m.Id, m.CustomerId, m.LdapGroupDn, m.RoleId, m.CreatedAt);
}
