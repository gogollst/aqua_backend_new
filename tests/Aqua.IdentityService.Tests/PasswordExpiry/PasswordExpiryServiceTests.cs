using Aqua.IdentityService.PasswordExpiry;
using FluentAssertions;
using Xunit;

namespace Aqua.IdentityService.Tests.PasswordExpiry;

public class PasswordExpiryServiceTests
{
    [Fact]
    public void IsExpired_NullLastChange_ReturnsTrue()
    {
        var sut = new PasswordExpiryService();
        sut.IsExpired(null).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Today_ReturnsFalse()
    {
        var sut = new PasswordExpiryService();
        sut.IsExpired(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_91DaysAgo_ReturnsTrue()
    {
        var sut = new PasswordExpiryService();
        sut.IsExpired(DateTime.UtcNow.AddDays(-91)).Should().BeTrue();
    }

    [Fact]
    public void GetExpiryDate_Null_ReturnsApproximatelyNowPlus90Days()
    {
        var sut = new PasswordExpiryService();
        var before = DateTime.UtcNow;
        var expiry = sut.GetExpiryDate(null);
        var after = DateTime.UtcNow;
        expiry.Should().BeOnOrAfter(before.AddDays(90).AddSeconds(-1));
        expiry.Should().BeOnOrBefore(after.AddDays(90).AddSeconds(1));
    }
}
