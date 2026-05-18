using Aqua.Contracts;
using FluentAssertions;
using Xunit;

namespace Aqua.Contracts.Tests;

public class TenantIdTests
{
    [Fact]
    public void Constructor_ValidValue_StoresValue()
    {
        var id = new TenantId("acme-corp");
        id.Value.Should().Be("acme-corp");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_InvalidValue_Throws(string? value)
    {
        var act = () => new TenantId(value!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        TenantId id = new TenantId("acme");
        string s = id;
        s.Should().Be("acme");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        new TenantId("a").Should().Be(new TenantId("a"));
    }
}
