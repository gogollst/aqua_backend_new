using Aqua.UserService.Events;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Aqua.UserService.Tests.Events;

public sealed class EventEnvelopeTests
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Envelope_round_trips_json()
    {
        var envelope = EventEnvelope.Create(
            tenantId: 42L,
            correlationId: "corr-1",
            causationId:   "cause-1",
            actor: new Actor("user", 17L),
            data: new UserRoleChanged(UserId: 99L,
                                      OldRoleIds: new[] { 1L },
                                      NewRoleIds: new[] { 1L, 2L },
                                      Source: "Admin"));
        var json = JsonSerializer.Serialize(envelope, JsonOpts);
        json.Should().Contain("\"eventType\":\"UserRoleChanged\"");
        json.Should().Contain("\"tenantId\":42");

        var back = JsonSerializer.Deserialize<EventEnvelope<UserRoleChanged>>(json, JsonOpts)!;
        back.EventType.Should().Be("UserRoleChanged");
        back.Data.UserId.Should().Be(99L);
        back.Data.NewRoleIds.Should().Equal(1L, 2L);
    }

    [Fact]
    public void Envelope_assigns_unique_event_id()
    {
        var a = EventEnvelope.Create(1L, "c", "x", new Actor("system"),
            new UserCreated(7L, "u", "e@x", false, false));
        var b = EventEnvelope.Create(1L, "c", "x", new Actor("system"),
            new UserCreated(7L, "u", "e@x", false, false));
        a.EventId.Should().NotBe(b.EventId);
    }
}
