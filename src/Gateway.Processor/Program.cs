using Gateway.Processor;
using Gateway.Processor.Services;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

// 1. Register the RabbitMQ Connection as a Singleton
builder.Services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory
{
    HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost",
    UserName = "admin",
    Password = "AdminPacs2026!"
});

// Create the persistent connection immediately
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = sp.GetRequiredService<IConnectionFactory>();
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// 2. Ensure Topology Exists (Queues, DLX) before consumer starts
var connection = builder.Services.BuildServiceProvider().GetRequiredService<IConnection>();
var topologySetup = new RabbitMqInfrastructureSetup();
await topologySetup.InitializeTopologyAsync(connection);

// 3. Register Services
builder.Services.AddSingleton<DashboardForwarder>();
builder.Services.AddHostedService<SecurityEventConsumer>();

var host = builder.Build();
host.Run();