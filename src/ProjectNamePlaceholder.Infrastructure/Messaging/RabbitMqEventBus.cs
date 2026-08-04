using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectNamePlaceholder.Application.Integration;
using RabbitMQ.Client;

namespace ProjectNamePlaceholder.Infrastructure.Messaging;

// Out-of-process IEventBus, selected via Integration:UseRabbitMq (see Program.cs). Publishes to a
// durable topic exchange, routing key = event type's full name; RabbitMqEventConsumer binds one
// durable queue per discovered IIntegrationEventHandler<T>. At-least-once, still no transactional
// outbox -- a publish that races a crash between DB commit and broker ack can still be lost, same
// caveat InProcessEventBus never had to worry about since it ran inside the caller's transaction.
public sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly Func<CancellationToken, Task<IConnection>> _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventBus(IConfiguration configuration, ILogger<RabbitMqEventBus> logger)
        : this(configuration, logger, ct => RabbitMqOptions.CreateConnectionAsync(configuration, ct))
    {
    }

    // Test seam: DI always resolves the constructor above, which dials a real broker.
    public RabbitMqEventBus(IConfiguration configuration, ILogger<RabbitMqEventBus> logger, Func<CancellationToken, Task<IConnection>> connectionFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionFactory = connectionFactory;
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent
    {
        var channel = await GetChannelAsync(cancellationToken);
        var exchange = RabbitMqOptions.ExchangeName(_configuration);
        var routingKey = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, typeof(TEvent));

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = routingKey
        };

        await channel.BasicPublishAsync(exchange, routingKey, mandatory: false, properties, body, cancellationToken);
        _logger.LogDebug("Published {EventType} to exchange {Exchange}", routingKey, exchange);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            _connection ??= await _connectionFactory(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.ExchangeDeclareAsync(RabbitMqOptions.ExchangeName(_configuration), ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
            return _channel;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }
}
