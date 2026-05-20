using Xunit;

namespace Aqua.UserService.Tests.TestSupport;

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
