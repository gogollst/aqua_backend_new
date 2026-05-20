using Aqua.UserService.Tenants;
using Aqua.UserService.Users;
using NHibernate;

namespace Aqua.UserService.Tests.TestSupport;

/// <summary>
/// Declarative seeder for integration tests. Adds tenants/users to an in-memory plan and persists
/// them in a single transaction on <see cref="PersistAsync"/>. Customers are deduplicated by slug
/// so multiple users in the same tenant share one <see cref="Customer"/> row.
/// </summary>
public sealed class TestSeed
{
    private readonly ISessionFactory _factory;
    private readonly List<(Customer Customer, User User)> _users = new();
    private readonly Dictionary<string, Customer> _customersBySlug = new();

    public long LastUserId { get; private set; }
    public long LastTenantId { get; private set; }
    public string LastTenantSlug { get; private set; } = "";

    public TestSeed(ISessionFactory factory) => _factory = factory;

    public TestSeed WithUser(string username, string customerSlug)
    {
        if (!_customersBySlug.TryGetValue(customerSlug, out var customer))
        {
            customer = new TenantBuilder().WithSlug(customerSlug).Build();
            _customersBySlug[customerSlug] = customer;
        }
        var user = new UserBuilder().WithUsername(username).Build();
        _users.Add((customer, user));
        LastTenantSlug = customerSlug;
        return this;
    }

    public async Task PersistAsync()
    {
        using var session = _factory.OpenSession();
        using var tx = session.BeginTransaction();
        // Persist customers first so we have generated ids before linking users.
        foreach (var c in _customersBySlug.Values)
        {
            await session.SaveAsync(c);
        }
        // Persist users with their owning customer id stamped into the denormalised hint column.
        foreach (var (c, u) in _users)
        {
            u.CustomerIdHint = c.Id;
            await session.SaveAsync(u);
            LastUserId = u.Id;
            LastTenantId = c.Id;
        }
        await tx.CommitAsync();
    }
}
