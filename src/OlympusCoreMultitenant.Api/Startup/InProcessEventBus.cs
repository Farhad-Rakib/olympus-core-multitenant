using OlympusCoreMultitenant.Application.Integration;

namespace OlympusCoreMultitenant.Api.Startup;

public sealed class InProcessEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InProcessEventBus> _logger;

    public InProcessEventBus(IServiceProvider serviceProvider, ILogger<InProcessEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent
    {
        var handlers = _serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>().ToList();
        if (handlers.Count == 0)
        {
            _logger.LogDebug("No handlers registered for {EventType}", typeof(TEvent).Name);
            return;
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(integrationEvent, cancellationToken);
        }
    }
}
