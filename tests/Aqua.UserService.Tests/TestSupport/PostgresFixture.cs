using NHibernate;
using Testcontainers.PostgreSql;
using Xunit;

namespace Aqua.UserService.Tests.TestSupport;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("aqua_users_test")
        .WithUsername("aqua")
        .WithPassword("aqua")
        .Build();

    public ISessionFactory SessionFactory { get; private set; } = null!;
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var builder = new Aqua.UserService.Persistence.UserServiceSessionFactoryBuilder(ConnectionString);
        SessionFactory = builder.Build();
    }

    public async Task DisposeAsync()
    {
        SessionFactory?.Dispose();
        await _container.DisposeAsync();
    }
}
