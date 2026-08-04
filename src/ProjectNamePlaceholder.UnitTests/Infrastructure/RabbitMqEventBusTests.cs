using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProjectNamePlaceholder.Application.Integration;
using ProjectNamePlaceholder.Infrastructure.Messaging;
using RabbitMQ.Client;

namespace ProjectNamePlaceholder.UnitTests.Infrastructure;

public class RabbitMqEventBusTests
{
    private sealed record TestEvent(string Payload) : IIntegrationEvent;

    private static IConfiguration CreateConfiguration(string? exchangeName = null)
    {
        var data = new Dictionary<string, string?>();
        if (exchangeName is not null)
        {
            data["Integration:RabbitMq:ExchangeName"] = exchangeName;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static (RabbitMqEventBus Bus, IChannel Channel) CreateBusWithFakeChannel(IConfiguration configuration)
    {
        var channel = Substitute.For<IChannel>();
        channel.IsOpen.Returns(true);

        var connection = Substitute.For<IConnection>();
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(channel));

        var bus = new RabbitMqEventBus(configuration, NullLogger<RabbitMqEventBus>.Instance, _ => Task.FromResult(connection));
        return (bus, channel);
    }

    [Fact]
    public async Task PublishAsync_publishes_to_the_configured_exchange_with_the_event_type_as_routing_key()
    {
        var configuration = CreateConfiguration("custom.exchange");
        var (bus, channel) = CreateBusWithFakeChannel(configuration);

        await bus.PublishAsync(new TestEvent("hello"));

        await channel.Received(1).BasicPublishAsync(
            "custom.exchange",
            typeof(TestEvent).FullName!,
            false,
            Arg.Any<BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_defaults_the_exchange_name_when_unconfigured()
    {
        var configuration = CreateConfiguration();
        var (bus, channel) = CreateBusWithFakeChannel(configuration);

        await bus.PublishAsync(new TestEvent("hello"));

        await channel.Received(1).BasicPublishAsync(
            "olympus.events",
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_serializes_the_event_as_the_message_body()
    {
        var configuration = CreateConfiguration();
        var (bus, channel) = CreateBusWithFakeChannel(configuration);

        await bus.PublishAsync(new TestEvent("hello"));

        var call = channel.ReceivedCalls().Single(c => c.GetMethodInfo().Name == nameof(IChannel.BasicPublishAsync));
        var body = (ReadOnlyMemory<byte>)call.GetArguments()[4]!;
        var json = System.Text.Encoding.UTF8.GetString(body.Span);
        Assert.Contains("hello", json);
    }
}
