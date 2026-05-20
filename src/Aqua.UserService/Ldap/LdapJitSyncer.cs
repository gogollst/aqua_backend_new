using Aqua.UserService.Domain;
using Aqua.UserService.Events;
using Aqua.UserService.Ldap.Dto;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants;
using Aqua.UserService.Users;

namespace Aqua.UserService.Ldap;

public interface ILdapJitSyncer
{
    Task<LdapJitSyncResponse> SyncAsync(LdapJitSyncRequest req, CancellationToken ct = default);
}

public sealed class LdapJitSyncer : ILdapJitSyncer
{
    private readonly ICustomerRepository _customers;
    private readonly IUserRepository _users;
    private readonly ILdapGroupRoleMappingRepository _mappings;
    private readonly ICustomerUserAssignmentRepository _cuas;
    private readonly IRoleRepository _roles;
    private readonly IUserEventPublisher _publisher;

    public LdapJitSyncer(
        ICustomerRepository customers,
        IUserRepository users,
        ILdapGroupRoleMappingRepository mappings,
        ICustomerUserAssignmentRepository cuas,
        IRoleRepository roles,
        IUserEventPublisher publisher)
    {
        _customers = customers;
        _users = users;
        _mappings = mappings;
        _cuas = cuas;
        _roles = roles;
        _publisher = publisher;
    }

    public async Task<LdapJitSyncResponse> SyncAsync(LdapJitSyncRequest req, CancellationToken ct = default)
    {
        var customer = await _customers.FindBySlugAsync(req.CustomerSlug)
            ?? throw new NotFoundException("tenant.not-found", $"Tenant '{req.CustomerSlug}' not found.");

        var user = await _users.FindByLdapDnAsync(customer.Id, req.LdapDn);
        var isNewUser = user is null;
        if (user is null)
        {
            user = new User
            {
                Username = await ResolveUniqueUsername(req.Username),
                Email = req.Email,
                FirstName = req.FirstName,
                Surname = req.Surname,
                Status = UserStatus.Active,
                LdapDn = req.LdapDn,
                CustomerIdHint = customer.Id,
            };
            await _users.InsertAsync(user);
            await _publisher.PublishAsync(customer.Id, "user.created",
                new UserCreated(user.Id, user.Username, user.Email, IsLdap: true, IsFirstAdmin: false), ct);
        }
        else
        {
            // Update profile fields that may have changed in LDAP.
            user.Email = req.Email;
            user.FirstName = req.FirstName;
            user.Surname = req.Surname;
        }

        // Compute target roles = mappings ∩ user groups.
        var allMappings = await _mappings.ListAsync(customer.Id);
        var groupSet = new HashSet<string>(req.Groups, StringComparer.OrdinalIgnoreCase);
        var targetRoleIds = allMappings
            .Where(m => groupSet.Contains(m.LdapGroupDn))
            .Select(m => m.RoleId)
            .Distinct()
            .ToList();

        if (targetRoleIds.Count == 0)
            throw new ForbiddenException("ldap-jit.no-roles",
                $"User '{req.Username}' has no LDAP group mapped to a role in tenant '{req.CustomerSlug}'.");

        var currentRoleIds = await _cuas.GetRoleIdsAsync(customer.Id, user.Id);
        var oldSet = new HashSet<long>(currentRoleIds);
        var newSet = new HashSet<long>(targetRoleIds);

        if (!oldSet.SetEquals(newSet))
        {
            await _cuas.AssignRolesAsync(customer.Id, user.Id, targetRoleIds);
            await _publisher.PublishAsync(customer.Id, "user.role-changed",
                new UserRoleChanged(user.Id,
                    OldRoleIds: currentRoleIds.OrderBy(x => x).ToList(),
                    NewRoleIds: targetRoleIds.OrderBy(x => x).ToList(),
                    Source: "LdapSync"), ct);
        }

        var roles = await _roles.GetByIdsAsync(targetRoleIds);
        var bitset = PermissionBitset.None;
        foreach (var r in roles) bitset = PermissionBitset.From(bitset.Flags | r.Permissions.Flags);

        return new LdapJitSyncResponse(
            UserId: user.Id,
            Username: user.Username,
            Roles: roles.Select(r => r.Name).ToList(),
            PermsBitset: (long)bitset.Flags,
            IsNewUser: isNewUser);
    }

    private async Task<string> ResolveUniqueUsername(string requested)
    {
        if (await _users.FindByUsernameAsync(requested) is null) return requested;
        for (var i = 2; i < 100; i++)
        {
            var candidate = $"{requested}_{i}";
            if (await _users.FindByUsernameAsync(candidate) is null) return candidate;
        }
        throw new ConflictException("user.username-collision",
            $"Could not derive unique username from '{requested}'.");
    }
}
