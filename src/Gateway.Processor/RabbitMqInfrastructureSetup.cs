using RabbitMQ.Client;

namespace Gateway.Processor
{
    public class RabbitMqInfrastructureSetup
    {
        public async Task InitializeTopologyAsync(IConnection connection)
        {
            using var channel = await connection.CreateChannelAsync();

            // 1. Declare the Dead Letter Exchange and Queue
            await channel.ExchangeDeclareAsync("security.dlx", ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync("security.dlq", durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync("security.dlq", "security.dlx", routingKey: "event.failed");

            // 2. Declare the Main Exchange and Queue (with DLX arguments)
            var queueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", "security.dlx" },
            { "x-dead-letter-routing-key", "event.failed" }
        };

            await channel.ExchangeDeclareAsync("security.events.exchange", ExchangeType.Topic, durable: true);
            await channel.QueueDeclareAsync("security.events.queue", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);

            // Bind queue to listen to all event types (e.g., "event.doorForced", "event.peopleCount")
            await channel.QueueBindAsync("security.events.queue", "security.events.exchange", routingKey: "event.#");
        }
    }
}
