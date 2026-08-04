using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectNamePlaceholder.Application.Integration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ProjectNamePlaceholder.Infrastructure.Messaging;

// Discovers every IIntegrationEventHandler<T> in the loaded assemblies (same reflection-discovery
// spirit as ModuleHost -- no manual registration) and binds one durable queue per event type to
// RabbitMqEventBus's topic exchange. Queues are quorum queues with a delivery limit, so RabbitMQ
// itself tracks redelivery attempts and dead-letters a message once the limit is exceeded -- no
// hand-rolled retry-count bookkeeping needed here.
public sealed class RabbitMqEventConsumer : BackgroundService
{
    private const int MaxDeliveryAttempts = 5;

    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqEventConsumer> _logger;

    public RabbitMqEventConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<RabbitMqEventConsumer> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var eventTypes = DiscoverIntegrationEventTypes();
        if (eventTypes.Count == 0)
        {
            _logger.LogInformation("No IIntegrationEventHandler<T> implementations found -- RabbitMQ consumer has nothing to bind");
            return;
        }

        var connection = await RabbitMqOptions.CreateConnectionAsync(_configuration, stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        var exchange = RabbitMqOptions.ExchangeName(_configuration);
        var deadLetterExchange = $"{exchange}.dead-letter";

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync(deadLetterExchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        foreach (var eventType in eventTypes)
        {
            var routingKey = eventType.FullName ?? eventType.Name;
            var queueName = $"olympus.{routingKey}";
            var deadLetterQueueName = $"{queueName}.dead-letter";

            await channel.QueueDeclareAsync(deadLetterQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueBindAsync(deadLetterQueueName, deadLetterExchange, routingKey, cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-queue-type"] = "quorum",
                    ["x-delivery-limit"] = MaxDeliveryAttempts,
                    ["x-dead-letter-exchange"] = deadLetterExchange,
                    ["x-dead-letter-routing-key"] = routingKey
                },
                cancellationToken: stoppingToken);
            await channel.QueueBindAsync(queueName, exchange, routingKey, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, ea) => HandleMessageAsync(channel, eventType, ea, stoppingToken);
            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, cancellationToken: stoppingToken);

            _logger.LogInformation("Bound queue {Queue} to {Exchange} for {EventType}", queueName, exchange, routingKey);
        }

        // Keep the hosted service alive for the process lifetime -- the consumer callbacks registered
        // above do the actual work, this just waits for shutdown.
        var completion = new TaskCompletionSource();
        using var registration = stoppingToken.Register(() => completion.TrySetResult());
        await completion.Task;
    }

    private async Task HandleMessageAsync(IChannel channel, Type eventType, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            var integrationEvent = JsonSerializer.Deserialize(ea.Body.Span, eventType)
                ?? throw new InvalidOperationException($"Deserialized {eventType.Name} was null.");

            using var scope = _scopeFactory.CreateScope();
            var dispatchMethod = typeof(RabbitMqEventConsumer)
                .GetMethod(nameof(DispatchAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(eventType);

            await (Task)dispatchMethod.Invoke(null, [scope.ServiceProvider, integrationEvent, stoppingToken])!;

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Handler for {EventType} failed, requeuing (RabbitMQ dead-letters after {Max} attempts)", eventType.Name, MaxDeliveryAttempts);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }

    private static async Task DispatchAsync<TEvent>(IServiceProvider serviceProvider, object integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync((TEvent)integrationEvent, cancellationToken);
        }
    }

    private static List<Type> DiscoverIntegrationEventTypes()
    {
        var handlerInterface = typeof(IIntegrationEventHandler<>);
        var eventTypes = new HashSet<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                foreach (var implementedInterface in type.GetInterfaces())
                {
                    if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == handlerInterface)
                    {
                        eventTypes.Add(implementedInterface.GetGenericArguments()[0]);
                    }
                }
            }
        }

        return eventTypes.ToList();
    }
}
