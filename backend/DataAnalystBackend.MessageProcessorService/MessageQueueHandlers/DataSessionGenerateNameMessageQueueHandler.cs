using DataAnalystBackend.Shared.AgentAPIModels;
using DataAnalystBackend.Shared.Exceptions;
using DataAnalystBackend.Shared.Interfaces;
using DataAnalystBackend.Shared.MessagingProviders.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataAnalystBackend.MessageProcessorService.MessageQueueHandlers
{
    public class DataSessionGenerateNameMessageQueueHandler : IMessageQueueHandler
    {
        private IConnection _connection;
        private IChannel? _channel;
        private AsyncEventingBasicConsumer? _consumer;
        private string _prefix;
        private IHttpClientFactory _httpClientFactory;

        public void Dispose()
        {
            _consumer = null;
            _channel?.Dispose();
            _connection.Dispose();
        }

        public async Task InitializeQueue(ConnectionFactory connectionFactory, IServiceProvider hostProvider, IConfiguration configuration)
        {
            using IServiceScope serviceScope = hostProvider.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;
            _prefix = configuration.GetValue<string>("RabbitMQ:Prefix");
            _httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();

            Console.WriteLine($"Initializing {nameof(DataSessionGenerateNameMessageQueueHandler)}");
            _connection = await connectionFactory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(queue: $"{_prefix}-{IMessagingProvider.DATA_SESSION_GENERATE_NAME}", durable: false, exclusive: false, autoDelete: false,
                arguments: null);


            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += Consume;

            Console.WriteLine($"Initialized {nameof(DataSessionGenerateNameMessageQueueHandler)}");
        }

        private async Task Consume(object sender, BasicDeliverEventArgs ea)
        {
            Console.WriteLine("Got Request for Name");
            string name = "Michael Test";
            AsyncEventingBasicConsumer cons = (AsyncEventingBasicConsumer)sender;
            IChannel ch = cons.Channel;

            IReadOnlyBasicProperties props = ea.BasicProperties;
            var replyProps = new BasicProperties
            {
                CorrelationId = props.CorrelationId
            };
            HttpClient agentClient = _httpClientFactory.CreateClient("AgentClient");

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Message<GeneralAgentProcessingRequest> deserializedMessage = JsonSerializer.Deserialize<Message<GeneralAgentProcessingRequest>>(message);

            var request = new
            {
                userId = deserializedMessage.Data.UserId,
                dataSessionId = deserializedMessage.Data.DataSessionId
            };

            string requestJson = JsonSerializer.Serialize(request);
            StringContent stringContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await agentClient.PostAsync("/api/agent/get-session-name", stringContent);

            if (response.IsSuccessStatusCode)
            {
                string dataSessionNameJson = await response.Content.ReadAsStringAsync();

                GetSessionNameModel dataSessionName = JsonSerializer.Deserialize<GetSessionNameModel>(dataSessionNameJson);

                GenerateNameResponseMessage generateNameResponseMessage = new GenerateNameResponseMessage()
                {
                    DataSessionName = dataSessionName.DataSessionName
                };

                string json = JsonSerializer.Serialize(generateNameResponseMessage);

                var responseBytes = Encoding.UTF8.GetBytes(json);
                await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
                    mandatory: true, basicProperties: replyProps, body: responseBytes);
                await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }

            Console.WriteLine("Completed Request for Name");
        }

        public async Task StartConsuming()
        {
            Console.WriteLine($"Starting Consuming on {nameof(DataSessionGenerateNameMessageQueueHandler)}");
            if (_consumer == null)
                throw new MessageProviderException($"{IMessagingProvider.DATA_SESSION_GENERATE_NAME} consumer not initialized");

            if (_channel == null)
                throw new MessageProviderException($"{IMessagingProvider.DATA_SESSION_GENERATE_NAME} channel not initialized");

            await _channel.BasicConsumeAsync($"{_prefix}-{IMessagingProvider.DATA_SESSION_GENERATE_NAME}", autoAck: true, consumer: _consumer);
        }
    }
}
