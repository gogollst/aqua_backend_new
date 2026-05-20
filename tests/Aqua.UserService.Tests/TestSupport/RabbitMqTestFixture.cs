using Testcontainers.RabbitMq;
using Xunit;

namespace Aqua.UserService.Tests.TestSupport;

/// <summary>
/// Spins up a throwaway RabbitMQ broker (management image) for the duration of a test class.
/// Exposes the auto-generated AMQP URI via <see cref="ConnectionString"/>; consumers should feed
/// it into <c>new ConnectionFactory { Uri = new Uri(...) }</c>.
///
/// Currently used by <c>RabbitMqSmokeTest</c> to verify the testcontainer integrates and the
/// broker is reachable. The SS-09 wave will wire MassTransit's RabbitMQ transport on top of this
/// fixture for outbox-relay integration tests.
/// </summary>
public sealed class RabbitMqTestFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync()    => await _container.DisposeAsync();
}
