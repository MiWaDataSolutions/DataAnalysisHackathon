using DataAnalystBackend.Hubs;
using DataAnalystBackend.Shared.DataAccess.Models;
using DataAnalystBackend.Shared.Interfaces;
using DataAnalystBackend.Shared.Interfaces.Services;
using DataAnalystBackend.Shared.MessagingProviders.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace DataAnalystBackend.Consumers
{
    public class DataSessionNameResponseConsumer : BackgroundService
    {
        private IConnection _connection;
        private IChannel? _channel;
        private readonly IHubContext<DataSessionHub> _hubContext;
        private readonly string _prefix;
        private readonly string _url;

        public DataSessionNameResponseConsumer(IHubContext<DataSessionHub> hubContext, IConfiguration configuration)
        {
            _hubContext = hubContext;
            _prefix = configuration.GetRequiredSection("RabbitMQ:Prefix").Value;
            _url = configuration.GetRequiredSection("RabbitMQ:HostName").Value;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory() { Uri = new Uri(_url) }; // Set your RabbitMQ host
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.QueueDeclareAsync(queue: $"{_prefix}-{IMessagingProvider.DATA_SESSION_GENERATE_NAME}_response", durable: false, exclusive: false, autoDelete: false, arguments: null);
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
                GenerateNameResponseMessage dataNameModel = JsonSerializer.Deserialize<GenerateNameResponseMessage>(message);
                using (var scope = ServiceProviderAccessor.RootServiceProvider.CreateScope())
                {
                    IDataSessionService dataSessionService = scope.ServiceProvider.GetRequiredService<IDataSessionService>();
                    IMessagingProvider messagingProvider = scope.ServiceProvider.GetRequiredService<IMessagingProvider>();
                    await dataSessionService.UpdateDataSession(dataNameModel.DataSessionId, dataNameModel.DataSessionName, dataNameModel.UserId);
                    await _hubContext.Clients.Group(dataNameModel.UserId).SendAsync("RecieveDataSessionName", dataNameModel.DataSessionId, dataNameModel.DataSessionName); ;
                    await messagingProvider.PublishMessageAsync(new Message<GenerateNameResponseMessage>() { Data = dataNameModel, MessageType = Shared.MessagingProviders.Models.Enums.MessageType.DataSessionDataProcess });
                }

            };
            return _channel.BasicConsumeAsync(queue: $"{_prefix}-{IMessagingProvider.DATA_SESSION_GENERATE_NAME}_response", autoAck: true, consumer: consumer);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection.Dispose();
        }
    }
}
