using System.Data;
using Aqua.UserService.Persistence.UserTypes;
using Aqua.UserService.Roles;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Persistence;

public sealed class PermissionBitsetUserTypeTests
{
    private readonly PermissionBitsetUserType _type = new();

    [Fact]
    public void ReturnedType_is_PermissionBitset()
    {
        _type.ReturnedType.Should().Be(typeof(PermissionBitset));
    }

    [Fact]
    public void SqlTypes_is_single_string()
    {
        _type.SqlTypes.Should().HaveCount(1);
        _type.SqlTypes[0].DbType.Should().Be(DbType.String);
    }

    [Fact]
    public void Equals_compares_by_flags()
    {
        var a = PermissionBitset.From(Permission.ReadRequirement);
        var b = PermissionBitset.From(Permission.ReadRequirement);
        var c = PermissionBitset.From(Permission.WriteRequirement);
        _type.Equals(a, b).Should().BeTrue();
        _type.Equals(a, c).Should().BeFalse();
        _type.Equals(null, null).Should().BeTrue();
    }

    [Fact]
    public void IsMutable_false()
    {
        _type.IsMutable.Should().BeFalse();
    }

    [Fact]
    public void DeepCopy_returns_same_instance_since_immutable()
    {
        var pb = PermissionBitset.From(Permission.ReadRequirement);
        _type.DeepCopy(pb).Should().BeSameAs(pb);
    }
}
