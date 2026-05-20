using Npgsql;
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
        await ApplyBaselineSchemaAsync();
        var builder = new Aqua.UserService.Persistence.UserServiceSessionFactoryBuilder(ConnectionString);
        SessionFactory = builder.Build();
    }

    public async Task DisposeAsync()
    {
        SessionFactory?.Dispose();
        await _container.DisposeAsync();
    }

    private async Task ApplyBaselineSchemaAsync()
    {
        var sqlPath = Path.Combine(AppContext.BaseDirectory, "Sql", "baseline-schema.sql");
        if (!File.Exists(sqlPath))
        {
            // Tests that need the baseline schema will fail explicitly with a clear missing-table
            // error; tests that don't need it (e.g. SELECT 1, filter-definition checks) still pass.
            return;
        }
        var sql = await File.ReadAllTextAsync(sqlPath);
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
