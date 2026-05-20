using NHibernate;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Tenants;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly ISession _session;
    public CustomerRepository(ISession session) => _session = session;

    public async Task<Customer?> FindByIdAsync(long id) =>
        await _session.GetAsync<Customer>(id);

    public async Task<Customer?> FindBySlugAsync(string slug) =>
        await _session.Query<Customer>().FirstOrDefaultAsync(c => c.Slug == slug);

    public async Task<IReadOnlyList<Customer>> ListAsync() =>
        await _session.Query<Customer>().OrderBy(c => c.Slug).ToListAsync();

    public Task InsertAsync(Customer customer) => _session.SaveAsync(customer);
}
