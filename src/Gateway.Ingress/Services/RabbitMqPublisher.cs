using Gateway.Core.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Gateway.Ingress.Services;

public interface IRabbitMqPublisher
{
    Task PublishEventAsync(SecurityEvent securityEvent);
}

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly ConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        // In a real environment, pull this from user secrets or key vault
        _connectionFactory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = "admin",
            Password = "AdminPacs2026!"
        };
    }

    private async Task EnsureConnectionAsync()
    {
        if (_connection is null || _channel is null)
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
        }
    }

    public async Task PublishEventAsync(SecurityEvent securityEvent)
    {
        await EnsureConnectionAsync();

        var message = JsonSerializer.Serialize(securityEvent);
        var body = Encoding.UTF8.GetBytes(message);

        // We use the event type to create a dynamic routing key (e.g., "event.PeopleCountUpdate")
        var routingKey = $"event.{securityEvent.Type}";

        var properties = new BasicProperties { Persistent = true };

        await _channel!.BasicPublishAsync(
            exchange: "security.events.exchange",
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }
}