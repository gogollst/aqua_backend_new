using Aqua.Contracts.Events;
using FluentAssertions;
using Xunit;

namespace Aqua.Contracts.Tests;

public class IntegrationEventTests
{
    public sealed record TestCaseCreated(string Title, int Priority) : IntegrationEventBase;

    [Fact]
    public void NewEvent_HasGeneratedIdAndTimestamp()
    {
        var e = new TestCaseCreated("Login flow", 1);
        e.MessageId.Should().NotBeEmpty();
        e.OccurredAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void TwoEvents_HaveDistinctIds()
    {
        var a = new TestCaseCreated("a", 1);
        var b = new TestCaseCreated("b", 1);
        a.MessageId.Should().NotBe(b.MessageId);
    }

    [Fact]
    public void Metadata_DefaultsToEmpty()
    {
        var e = new TestCaseCreated("x", 1);
        e.Metadata.Should().NotBeNull();
        e.Metadata.Headers.Should().BeEmpty();
    }
}
