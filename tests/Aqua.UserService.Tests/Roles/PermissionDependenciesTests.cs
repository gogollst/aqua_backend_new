using Aqua.UserService.Roles;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Roles;

public sealed class PermissionDependenciesTests
{
    [Theory]
    [InlineData(Permission.WriteRequirement, Permission.ReadRequirement)]
    [InlineData(Permission.WriteTestCase,    Permission.ReadTestCase)]
    [InlineData(Permission.WriteDefect,      Permission.ReadDefect)]
    [InlineData(Permission.ExecuteTest,      Permission.ReadTestCase)]
    [InlineData(Permission.ManageUsers,      Permission.ReadUser)]
    [InlineData(Permission.ManageRoles,      Permission.ReadRole)]
    public void Write_and_manage_perms_imply_their_read_counterparts(Permission set, Permission implied)
    {
        var closure = PermissionDependencies.Close(set);
        closure.Should().HaveFlag(implied);
    }

    [Fact]
    public void Close_is_idempotent()
    {
        var once  = PermissionDependencies.Close(Permission.WriteRequirement);
        var twice = PermissionDependencies.Close(once);
        twice.Should().Be(once);
    }

    [Fact]
    public void Close_of_none_is_none()
    {
        PermissionDependencies.Close(Permission.None).Should().Be(Permission.None);
    }

    [Fact]
    public void AddedFlags_returns_diff()
    {
        var (closure, added) = PermissionDependencies.CloseWithDiff(Permission.WriteRequirement);
        closure.Should().HaveFlag(Permission.ReadRequirement);
        added.Should().Contain(Permission.ReadRequirement);
        added.Should().NotContain(Permission.WriteRequirement);
    }
}
