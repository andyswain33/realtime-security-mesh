using System.Text;
using System.Text.Json;
using Gateway.Core.Models;
using Gateway.Processor.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Gateway.Processor;

public class SecurityEventConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly DashboardForwarder _forwarder;
    private readonly ILogger<SecurityEventConsumer> _logger;

    public SecurityEventConsumer(IConnection connection, DashboardForwarder forwarder, ILogger<SecurityEventConsumer> logger)
    {
        _connection = connection;
        _forwarder = forwarder;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ensure our connection to the SignalR hub is established before we start pulling messages
        await _forwarder.StartAsync(stoppingToken);

        var channel = await _connection.CreateChannelAsync();
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 50, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var securityEvent = JsonSerializer.Deserialize<SecurityEvent>(message, options);

                if (securityEvent != null)
                {
                    // 1. Process Domain Logic (e.g., Update Redis occupancy cache here)

                    // 2. Push to the Real-Time UI
                    await _forwarder.BroadcastEventAsync(securityEvent);

                    _logger.LogInformation("Processed Event {EventId} for Zone {ZoneId}", securityEvent.EventId, securityEvent.ZoneId);
                }

                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Poison message detected. Routing to Dead Letter Exchange.");
                await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(queue: "security.events.queue", autoAck: false, consumer: consumer);
    }
}