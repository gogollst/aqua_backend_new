using System.Security.Cryptography;
using System.Text.Json;
using Aqua.UserService.Domain;
using Aqua.UserService.Events;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants.Dto;
using Aqua.UserService.Users;

namespace Aqua.UserService.Tenants;

public interface ITenantBootstrapper
{
    Task<BootstrapTenantResponse> BootstrapAsync(BootstrapTenantRequest req, CancellationToken ct = default);
}

public sealed class TenantBootstrapper : ITenantBootstrapper
{
    private readonly ICustomerRepository _customers;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ICustomerUserAssignmentRepository _cuas;
    private readonly IUserEventPublisher _publisher;

    public TenantBootstrapper(
        ICustomerRepository customers, IUserRepository users, IRoleRepository roles,
        ICustomerUserAssignmentRepository cuas, IUserEventPublisher publisher)
    {
        _customers = customers; _users = users; _roles = roles; _cuas = cuas; _publisher = publisher;
    }

    public async Task<BootstrapTenantResponse> BootstrapAsync(BootstrapTenantRequest req, CancellationToken ct = default)
    {
        var existing = await _customers.FindBySlugAsync(req.Slug);
        if (existing is not null)
        {
            var existingAdmin = await _users.FindByUsernameAsync(req.AdminUser.Username)
                ?? throw new ConflictException("tenant.bootstrap-conflict",
                    $"Tenant '{req.Slug}' exists but admin user '{req.AdminUser.Username}' does not.");
            if (existingAdmin.Email != req.AdminUser.Email)
                throw new ConflictException("tenant.bootstrap-conflict",
                    $"Tenant '{req.Slug}' exists but admin user has different email.");
            return new BootstrapTenantResponse(
                TenantId: existing.Id,
                TenantSlug: existing.Slug,
                AdminUserId: existingAdmin.Id,
                AdminUsername: existingAdmin.Username,
                InitialPassword: null,
                Skipped: true,
                RolesCreated: 0);
        }

        var customer = new Customer
        {
            Slug = req.Slug,
            DisplayName = req.DisplayName,
            PrimaryDomain = req.PrimaryDomain,
            AuthMode = req.Auth.Mode,
            AuthConfigJson = req.Auth.Mode switch
            {
                TenantAuthMode.Ldap => req.Auth.LdapConfigJson,
                TenantAuthMode.Saml => req.Auth.SamlConfigJson,
                _                   => null,
            },
        };
        await _customers.InsertAsync(customer);

        await _publisher.PublishAsync(customer.Id, "tenant.created",
            new TenantCreated(customer.Id, customer.Slug, customer.DisplayName), ct);

        var rolesCreated = 0;
        if (req.DefaultRoles != "none")
        {
            var roleDefs = LoadDefaultRoleSet(req.DefaultRoles);
            foreach (var rd in roleDefs)
            {
                var role = new Role
                {
                    Name = rd.Name,
                    Description = rd.Description,
                    CustomerId = customer.Id,
                    AvailableInProject = rd.AvailableInProject,
                    AvailableInCustomer = rd.AvailableInCustomer,
                    IsDefault = rd.IsDefault,
                    Permissions = ParsePermissions(rd.Permissions),
                };
                await _roles.InsertAsync(role);
                rolesCreated++;
            }
        }

        var initialPassword = req.AdminUser.PasswordMode == "generate"
            ? GeneratePassword()
            : req.AdminUser.Password
              ?? throw new BusinessRuleViolationException("admin.password-missing",
                  "passwordMode=provided requires password.");

        var admin = new User
        {
            Username = req.AdminUser.Username,
            Email = req.AdminUser.Email,
            FirstName = req.AdminUser.FirstName,
            Surname = req.AdminUser.Surname,
            Status = UserStatus.Active,
            ServerAdmin = true,
            CustomerIdHint = customer.Id,
        };
        await _users.InsertAsync(admin);

        await _publisher.PublishAsync(customer.Id, "user.created",
            new UserCreated(admin.Id, admin.Username, admin.Email,
                IsLdap: false, IsFirstAdmin: true), ct);

        return new BootstrapTenantResponse(
            TenantId: customer.Id,
            TenantSlug: customer.Slug,
            AdminUserId: admin.Id,
            AdminUsername: admin.Username,
            InitialPassword: req.AdminUser.PasswordMode == "generate" ? initialPassword : null,
            Skipped: false,
            RolesCreated: rolesCreated);
    }

    private static IReadOnlyList<DefaultRoleDef> LoadDefaultRoleSet(string name)
    {
        var asm = typeof(TenantBootstrapper).Assembly;
        var resName = $"Aqua.UserService.Tenants.Resources.DefaultRoleSets.{name}.json";
        using var stream = asm.GetManifestResourceStream(resName)
            ?? throw new BusinessRuleViolationException("tenant.unknown-default-roles",
                $"Unknown defaultRoles set '{name}'.");
        using var reader = new StreamReader(stream);
        return JsonSerializer.Deserialize<List<DefaultRoleDef>>(reader.ReadToEnd(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? new List<DefaultRoleDef>();
    }

    private static PermissionBitset ParsePermissions(IReadOnlyList<string> tokens)
    {
        var flags = Permission.None;
        foreach (var t in tokens)
            if (Enum.TryParse<Permission>(t, ignoreCase: true, out var p)) flags |= p;
        return PermissionBitset.From(flags);
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%&*-+=";
        var bytes = RandomNumberGenerator.GetBytes(16);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private sealed record DefaultRoleDef(
        string Name,
        string? Description,
        List<string> Permissions,
        bool AvailableInProject,
        bool AvailableInCustomer,
        bool IsDefault);
}
