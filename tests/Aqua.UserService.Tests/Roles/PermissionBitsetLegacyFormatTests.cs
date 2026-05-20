using Aqua.UserService.Roles;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Roles;

public sealed class PermissionBitsetLegacyFormatTests
{
    [Fact]
    public void Empty_blob_yields_None()
    {
        PermissionBitset.FromLegacyBlob("").Flags.Should().Be(Permission.None);
        PermissionBitset.FromLegacyBlob("   ").Flags.Should().Be(Permission.None);
    }

    [Fact]
    public void Known_token_round_trip()
    {
        var blob = "ReadRequirement,WriteRequirement";
        var b    = PermissionBitset.FromLegacyBlob(blob);
        b.Has(Permission.ReadRequirement).Should().BeTrue();
        b.Has(Permission.WriteRequirement).Should().BeTrue();
        b.ToLegacyBlob().Should().Be("ReadRequirement,WriteRequirement");
    }

    [Fact]
    public void Unknown_token_is_ignored()
    {
        var b = PermissionBitset.FromLegacyBlob("ReadRequirement,FlyAirplane,WriteRequirement");
        b.UnknownTokens.Should().BeEquivalentTo("FlyAirplane");
        b.Has(Permission.ReadRequirement).Should().BeTrue();
        b.Has(Permission.WriteRequirement).Should().BeTrue();
    }

    [Fact]
    public void Tokens_are_case_insensitive()
    {
        var b = PermissionBitset.FromLegacyBlob("readrequirement, WriteRequirement");
        b.Has(Permission.ReadRequirement).Should().BeTrue();
        b.Has(Permission.WriteRequirement).Should().BeTrue();
    }

    [Fact]
    public void Output_is_sorted_by_bit_value_for_determinism()
    {
        var input = PermissionBitset.From(Permission.WriteRequirement | Permission.ReadRequirement);
        input.ToLegacyBlob().Should().Be("ReadRequirement,WriteRequirement");
    }
}
