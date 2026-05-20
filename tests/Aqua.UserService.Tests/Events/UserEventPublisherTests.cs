using Aqua.Data.Outbox;
using Aqua.UserService.Events;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Events;

public sealed class UserEventPublisherTests
{
    private readonly IOutboxWriter _outbox = Substitute.For<IOutboxWriter>();

    private UserEventPublisher CreateSut(
        string correlationId = "corr-1",
        string causationId   = "cause-1",
        Actor?  actor        = null)
    {
        return new UserEventPublisher(_outbox,
            () => correlationId,
            () => causationId,
            () => actor ?? new Actor("user", 17L));
    }

    [Fact]
    public async Task PublishAsync_writes_envelope_to_outbox_with_routing_key()
    {
        OutboxIntegrationEvent<UserCreated>? captured = null;
        await _outbox.WriteAsync(Arg.Do<OutboxIntegrationEvent<UserCreated>>(e => captured = e));

        var sut = CreateSut();
        await sut.PublishAsync(
            tenantId:   42L,
            routingKey: "user.created",
            data:       new UserCreated(7L, "alice", "alice@x.com", IsLdap: false, IsFirstAdmin: false));

        await _outbox.Received(1).WriteAsync(
            Arg.Any<OutboxIntegrationEvent<UserCreated>>(), Arg.Any<CancellationToken>());

        captured.Should().NotBeNull();
        captured!.RoutingKey.Should().Be("user.created");
        captured.ExchangeName.Should().Be(UserEventPublisher.ExchangeName);
        captured.Envelope.TenantId.Should().Be(42L);
        captured.Envelope.CorrelationId.Should().Be("corr-1");
        captured.Envelope.CausationId.Should().Be("cause-1");
        captured.Envelope.Actor.UserId.Should().Be(17L);
        captured.Envelope.Data.Username.Should().Be("alice");
    }

    [Fact]
    public async Task PublishAsync_propagates_routing_key_through_event_metadata_headers()
    {
        OutboxIntegrationEvent<RoleUpdated>? captured = null;
        await _outbox.WriteAsync(Arg.Do<OutboxIntegrationEvent<RoleUpdated>>(e => captured = e));

        var sut = CreateSut();
        await sut.PublishAsync(tenantId: 9L, routingKey: "role.updated",
            data: new RoleUpdated(RoleId: 3L, TenantId: 9L));

        captured.Should().NotBeNull();
        captured!.Metadata.Headers[UserEventPublisher.ExchangeHeader]
            .Should().Be(UserEventPublisher.ExchangeName);
        captured.Metadata.Headers[UserEventPublisher.RoutingKeyHeader]
            .Should().Be("role.updated");
        captured.Metadata.Headers["X-Aqua-Causation-Id"].Should().Be("cause-1");
        captured.Metadata.TenantId.Should().Be("9");
        captured.Metadata.CorrelationId.Should().Be("corr-1");
        captured.Metadata.OriginalUserId.Should().Be("17");
    }

    [Fact]
    public async Task PublishAsync_with_empty_routing_key_throws()
    {
        var sut = CreateSut();

        var act = () => sut.PublishAsync(1L, "", new UserDeleted(1L));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Routing key*");
        await _outbox.DidNotReceive().WriteAsync(
            Arg.Any<OutboxIntegrationEvent<UserDeleted>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_system_actor_omits_original_user_header()
    {
        OutboxIntegrationEvent<TenantUpdated>? captured = null;
        await _outbox.WriteAsync(Arg.Do<OutboxIntegrationEvent<TenantUpdated>>(e => captured = e));

        var sut = CreateSut(actor: new Actor("system"));
        await sut.PublishAsync(1L, "tenant.updated",
            new TenantUpdated(1L, new[] { "DisplayName" }));

        captured.Should().NotBeNull();
        captured!.Metadata.Headers.ContainsKey("X-Aqua-Original-User").Should().BeFalse();
        captured.Envelope.Actor.Type.Should().Be("system");
        captured.Envelope.Actor.UserId.Should().BeNull();
    }
}
