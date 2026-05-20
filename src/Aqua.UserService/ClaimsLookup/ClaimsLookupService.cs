using Aqua.UserService.ClaimsLookup.Dto;
using Aqua.UserService.Domain;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants;
using Aqua.UserService.Users;

namespace Aqua.UserService.ClaimsLookup;

public interface IClaimsLookupService
{
    Task<ClaimsLookupResponse> LookupAsync(long userId, string tenantSlug);
}

public sealed class ClaimsLookupService : IClaimsLookupService
{
    private readonly IUserRepository _users;
    private readonly ICustomerRepository _customers;
    private readonly ICustomerUserAssignmentRepository _cuas;
    private readonly IRoleRepository _roles;

    public ClaimsLookupService(
        IUserRepository users,
        ICustomerRepository customers,
        ICustomerUserAssignmentRepository cuas,
        IRoleRepository roles)
    {
        _users = users;
        _customers = customers;
        _cuas = cuas;
        _roles = roles;
    }

    public async Task<ClaimsLookupResponse> LookupAsync(long userId, string tenantSlug)
    {
        var customer = await _customers.FindBySlugAsync(tenantSlug)
            ?? throw new NotFoundException("tenant.not-found", $"Tenant '{tenantSlug}' not found.");
        var user = await _users.FindByIdAsync(userId)
            ?? throw NotFoundException.ForUser(userId);

        var roleIds = await _cuas.GetRoleIdsAsync(customer.Id, user.Id);
        var roles   = await _roles.GetByIdsAsync(roleIds);

        var bitset = PermissionBitset.None;
        foreach (var r in roles)
        {
            bitset = PermissionBitset.From(bitset.Flags | r.Permissions.Flags);
        }

        return new ClaimsLookupResponse(
            Sub:         user.Id.ToString(),
            Name:        $"{user.FirstName} {user.Surname}".Trim(),
            Email:       user.Email,
            TenantId:    customer.Id,
            TenantSlug:  customer.Slug,
            IsActive:    user.Status == UserStatus.Active && !user.Deleted,
            ServerAdmin: user.ServerAdmin,
            Roles:       roles.Select(r => r.Name).ToList(),
            PermsBitset: (long)bitset.Flags);
    }
}
