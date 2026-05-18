using Aqua.IdentityService.Lockout;
using FluentAssertions;
using Xunit;

namespace Aqua.IdentityService.Tests.Lockout;

public class AccountLockoutServiceTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(99, true)]
    public void ShouldLock_AfterThreshold(int failedAttempts, bool expectedLocked)
    {
        var sut = new AccountLockoutService();
        sut.ShouldLock(failedAttempts).Should().Be(expectedLocked);
    }

    [Fact]
    public void LockoutUntil_IsThirtyMinutesFromNow()
    {
        var sut = new AccountLockoutService();
        var before = DateTime.UtcNow;
        var until = sut.GetLockoutUntil();
        var after = DateTime.UtcNow;
        until.Should().BeOnOrAfter(before.AddMinutes(30).AddSeconds(-1));
        until.Should().BeOnOrBefore(after.AddMinutes(30).AddSeconds(1));
    }
}
