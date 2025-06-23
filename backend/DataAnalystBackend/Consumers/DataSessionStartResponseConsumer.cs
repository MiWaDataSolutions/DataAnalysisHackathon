
using DataAnalystBackend.Hubs;
using DataAnalystBackend.Shared.Interfaces;
using DataAnalystBackend.Shared.MessagingProviders.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace DataAnalystBackend.Consumers
{
    public class DataSessionStartResponseConsumer : BackgroundService
    {
        private IConnection _connection;
        private IChannel? _channel;
        private readonly string _prefix;
        private readonly string _url;

        public DataSessionStartResponseConsumer(IConfiguration configuration)
        {
            _prefix = configuration.GetRequiredSection("RabbitMQ:Prefix").Value;
            _url = configuration.GetRequiredSection("RabbitMQ:HostName").Value;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory() { Uri = new Uri(_url) }; // Set your RabbitMQ host
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.QueueDeclareAsync(queue: $"{_prefix}-{IMessagingProvider.DATA_SESSION_START}_response", durable: false, exclusive: false, autoDelete: false, arguments: null);
            await base.StartAsync(cancellationToken);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                // Handle the message
                Console.WriteLine($"Received: {message}");
                StartDataSessionMessage startDataSessionMessage = JsonSerializer.Deserialize<StartDataSessionMessage>(message);

                using (var scope = ServiceProviderAccessor.RootServiceProvider.CreateScope())
                {
                    IMessagingProvider messagingProvider = scope.ServiceProvider.GetRequiredService<IMessagingProvider>();
                    await messagingProvider.PublishMessageAsync(new Message<StartDataSessionMessage>() { Data = startDataSessionMessage, MessageType = Shared.MessagingProviders.Models.Enums.MessageType.DataSessionGenerateName });
                }
            };
            return _channel.BasicConsumeAsync(queue: $"{_prefix}-{IMessagingProvider.DATA_SESSION_START}_response", autoAck: true, consumer: consumer);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection.Dispose();
        }
    }
}
