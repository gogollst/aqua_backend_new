using Aqua.UserService.Roles;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Roles;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void Catalog_contains_one_entry_per_named_permission()
    {
        var catalog = PermissionCatalog.Build();
        catalog.Entries.Should().NotBeEmpty();
        catalog.Entries.Should().OnlyHaveUniqueItems(e => e.Key);
        catalog.Entries.Should().OnlyHaveUniqueItems(e => e.Bit);
        catalog.Entries.Should().NotContain(e => e.Key == "None");
    }

    [Fact]
    public void Entries_include_labels_and_dependency_graph()
    {
        var entry = PermissionCatalog.Build().Entries.Single(e => e.Key == "WriteRequirement");
        entry.Implies.Should().Contain("ReadRequirement");
        entry.Labels.Should().ContainKey("en");
        entry.Labels.Should().ContainKey("de");
    }

    [Fact]
    public void Bit_values_match_enum()
    {
        foreach (var entry in PermissionCatalog.Build().Entries)
        {
            var enumValue = Enum.Parse<Permission>(entry.Key);
            ((long)enumValue).Should().Be(entry.Bit);
        }
    }
}
