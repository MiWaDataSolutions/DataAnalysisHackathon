﻿using DataAnalystBackend.Hubs;
using DataAnalystBackend.Shared.Interfaces;
using DataAnalystBackend.Shared.Interfaces.Services;
using DataAnalystBackend.Shared.MessagingProviders.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace DataAnalystBackend.Consumers
{
    public class DataProcessResponseConsumer : BackgroundService
    {
        private IConnection _connection;
        private IChannel? _channel;
        private readonly IHubContext<DataSessionHub> _hubContext;
        private readonly string _prefix;
        private readonly string _url;

        public DataProcessResponseConsumer(IHubContext<DataSessionHub> hubContext, IConfiguration configuration)
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
            await _channel.QueueDeclareAsync(queue: $"{_prefix}-{IMessagingProvider.DATA_SESSION_DATA_PROCESS}_response", durable: false, exclusive: false, autoDelete: false, arguments: null);
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
                DataProcessStartResponseMessage dataNameModel = JsonSerializer.Deserialize<DataProcessStartResponseMessage>(message);

                using (var scope = ServiceProviderAccessor.RootServiceProvider.CreateScope())
                {
                    IDataSessionFileService dataSessionFileService = scope.ServiceProvider.GetRequiredService<IDataSessionFileService>();
                    await dataSessionFileService.SetDataFileProcessed(dataNameModel.DataSessionId);
                }
                await _hubContext.Clients.Group(dataNameModel.UserId).SendAsync("RecieveDataSessionDataGenerationComplete", dataNameModel.DataSessionId);

            };
            return _channel.BasicConsumeAsync(queue: $"{_prefix}-{IMessagingProvider.DATA_SESSION_DATA_PROCESS}_response", autoAck: true, consumer: consumer);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection.Dispose();
        }
    }
}
