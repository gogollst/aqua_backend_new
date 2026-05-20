namespace Aqua.UserService.Tenants;

public interface ICustomerRepository
{
    Task<Customer?> FindByIdAsync(long id);
    Task<Customer?> FindBySlugAsync(string slug);
    Task<IReadOnlyList<Customer>> ListAsync();
    Task InsertAsync(Customer customer);
}
