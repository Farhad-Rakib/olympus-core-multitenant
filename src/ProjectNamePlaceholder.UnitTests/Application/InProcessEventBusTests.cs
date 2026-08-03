using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectNamePlaceholder.Api.Startup;
using ProjectNamePlaceholder.Application.Integration;

namespace ProjectNamePlaceholder.UnitTests.Application;

public class InProcessEventBusTests
{
    private sealed record TestEvent(string Payload) : IIntegrationEvent;

    private sealed class RecordingHandler : IIntegrationEventHandler<TestEvent>
    {
        public List<string> Received { get; } = new();

        public Task HandleAsync(TestEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            Received.Add(integrationEvent.Payload);
            return Task.CompletedTask;
        }
    }

    private static InProcessEventBus CreateBus(IServiceProvider serviceProvider) =>
        new(serviceProvider, NullLogger<InProcessEventBus>.Instance);

    [Fact]
    public async Task PublishAsync_invokes_a_registered_handler()
    {
        var handler = new RecordingHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventHandler<TestEvent>>(handler);
        var provider = services.BuildServiceProvider();

        await CreateBus(provider).PublishAsync(new TestEvent("hello"));

        Assert.Equal(["hello"], handler.Received);
    }

    [Fact]
    public async Task PublishAsync_invokes_every_registered_handler_for_the_event_type()
    {
        var handlerA = new RecordingHandler();
        var handlerB = new RecordingHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventHandler<TestEvent>>(handlerA);
        services.AddSingleton<IIntegrationEventHandler<TestEvent>>(handlerB);
        var provider = services.BuildServiceProvider();

        await CreateBus(provider).PublishAsync(new TestEvent("hello"));

        Assert.Equal(["hello"], handlerA.Received);
        Assert.Equal(["hello"], handlerB.Received);
    }

    [Fact]
    public async Task PublishAsync_does_not_throw_when_no_handler_is_registered()
    {
        var provider = new ServiceCollection().BuildServiceProvider();

        await CreateBus(provider).PublishAsync(new TestEvent("hello"));
    }
}
