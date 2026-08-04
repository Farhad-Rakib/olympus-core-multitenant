using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace ProjectNamePlaceholder.Infrastructure.Messaging;

internal static class RabbitMqOptions
{
    public static string ExchangeName(IConfiguration configuration) =>
        configuration["Integration:RabbitMq:ExchangeName"] ?? "olympus.events";

    public static async Task<IConnection> CreateConnectionAsync(IConfiguration configuration, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("Missing connection string: RabbitMq.");

        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true
        };

        return await factory.CreateConnectionAsync(cancellationToken);
    }
}
