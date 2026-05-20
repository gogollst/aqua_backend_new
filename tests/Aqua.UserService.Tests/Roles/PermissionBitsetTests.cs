using Aqua.UserService.Roles;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace Aqua.UserService.Tests.Roles;

public sealed class PermissionBitsetTests
{
    [Fact]
    public void None_has_no_flags()
    {
        PermissionBitset.None.Flags.Should().Be(Permission.None);
        PermissionBitset.None.Has(Permission.ReadRequirement).Should().BeFalse();
    }

    [Fact]
    public void From_flags_preserves_value()
    {
        var b = PermissionBitset.From(Permission.ReadRequirement | Permission.WriteRequirement);
        b.Has(Permission.ReadRequirement).Should().BeTrue();
        b.Has(Permission.WriteRequirement).Should().BeTrue();
        b.Has(Permission.ReadDefect).Should().BeFalse();
    }

    [Fact]
    public void EnforceDependencies_adds_implied_perms()
    {
        var input = PermissionBitset.From(Permission.WriteRequirement);
        var (closure, added) = input.EnforceDependencies();
        closure.Has(Permission.ReadRequirement).Should().BeTrue();
        added.Should().Contain(Permission.ReadRequirement);
    }

    [Property]
    public Property Roundtrip_blob_is_identity(int seed)
    {
        var rng = new Random(seed);
        var values = Enum.GetValues<Permission>().Where(v => v != Permission.None).ToArray();
        long bits = 0;
        foreach (var v in values) if (rng.Next(0, 2) == 1) bits |= (long)v;

        var original = PermissionBitset.From((Permission)bits);
        var blob     = original.ToLegacyBlob();
        var restored = PermissionBitset.FromLegacyBlob(blob);
        return (original.Flags == restored.Flags).ToProperty();
    }
}
