using DataAnalystBackend.Shared.Exceptions;
using DataAnalystBackend.Shared.Interfaces;
using DataAnalystBackend.Shared.MessagingProviders.Models;
using DataAnalystBackend.Shared.MessagingProviders.Models.Enums;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.MessagingProviders
{
    public class RabbitMQMessagingProvider : IMessagingProvider
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly string _prefix;

        public RabbitMQMessagingProvider(IConfiguration configuration)
        {

            _connectionFactory = new ConnectionFactory() { HostName = configuration["RabbitMQ:HostName"] };
            string? username = configuration.GetSection("RabbitMQ:Username").Value;
            string? password = configuration.GetSection("RabbitMQ:Password").Value;
            if (!string.IsNullOrWhiteSpace(username))
                _connectionFactory.UserName = username;

            if (!string.IsNullOrWhiteSpace(password))
                _connectionFactory.Password = password;

            _prefix = configuration.GetRequiredSection("RabbitMQ:Prefix").Value;
        }

        public async Task PublishMessageAsync<TMessage>(Message<TMessage> message)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            string queueName = string.Empty;
            switch (message.MessageType)
            {
                case MessageType.DataSessionNameGenerated:
                    queueName = $"{_prefix}-{IMessagingProvider.DATA_SESSION_NAME_GENERATED}";
                    break;
                case MessageType.DataSessionGenerateName:
                    queueName = $"{_prefix}-{IMessagingProvider.DATA_SESSION_GENERATE_NAME}";
                    break;
                default:
                    throw new MessageProviderException($"Message Type {message.MessageType} not supported");
            }

            await channel.QueueDeclareAsync(queue: queueName, exclusive: false, autoDelete: false);

            string messageJson = JsonSerializer.Serialize(message);
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, body: messageBytes);
        }

        public async Task PublishMessageAsync(Message message)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            string queueName = string.Empty;
            switch (message.MessageType)
            {
                default:
                    throw new MessageProviderException($"Message Type {message.MessageType} not supported");
            }

            await channel.QueueDeclareAsync(queue: queueName, exclusive: false, autoDelete: false);

            string messageJson = JsonSerializer.Serialize(message);
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, body: messageBytes);
        }
    }
}
