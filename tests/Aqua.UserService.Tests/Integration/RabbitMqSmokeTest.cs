using Aqua.UserService.Tests.TestSupport;
using FluentAssertions;
using RabbitMQ.Client;
using Xunit;

namespace Aqua.UserService.Tests.Integration;

/// <summary>
/// Minimum-viable smoke test for <see cref="RabbitMqTestFixture"/>. Opens an AMQP connection,
/// declares a topic exchange, asserts the connection is open. ExchangeDeclareAsync throws on
/// failure, so no extra assertion is needed beyond reaching this point.
///
/// Tagged <c>BrokerSmoke</c> so the default CI command can exclude it and only opt-in runs
/// (Docker available) execute it. Production code never touches RabbitMQ directly — events are
/// written to <c>messaging_outbox</c> first, and the broker relay lives in SS-09 Wave.
/// </summary>
[Trait("Category", "BrokerSmoke")]
public sealed class RabbitMqSmokeTest : IClassFixture<RabbitMqTestFixture>
{
    private readonly RabbitMqTestFixture _rabbit;
    public RabbitMqSmokeTest(RabbitMqTestFixture r) => _rabbit = r;

    [Fact]
    public async Task Container_is_reachable_and_can_declare_exchange()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_rabbit.ConnectionString) };
        await using var conn = await factory.CreateConnectionAsync();
        await using var channel = await conn.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: "aqua.user-service.events",
            type: "topic",
            durable: true);

        conn.IsOpen.Should().BeTrue();
    }
}
