using Aqua.UserService.Tests.TestSupport;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace Aqua.UserService.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class MigrationApplicationTests
{
    private readonly PostgresFixture _fx;
    public MigrationApplicationTests(PostgresFixture fx) => _fx = fx;

    [Theory]
    [InlineData("aquauser", "ldap_dn")]
    [InlineData("role",     "version")]
    public async Task Migration_added_column(string table, string column)
    {
        await using var conn = new NpgsqlConnection(_fx.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 1 FROM information_schema.columns
            WHERE table_name = @t AND column_name = @c";
        cmd.Parameters.AddWithValue("t", table);
        cmd.Parameters.AddWithValue("c", column);
        var present = await cmd.ExecuteScalarAsync();
        present.Should().NotBeNull();
    }

    [Fact]
    public async Task ldap_group_role_mapping_table_exists()
    {
        await using var conn = new NpgsqlConnection(_fx.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT 1 FROM information_schema.tables WHERE table_name = 'ldap_group_role_mapping'";
        var present = await cmd.ExecuteScalarAsync();
        present.Should().NotBeNull();
    }
}
