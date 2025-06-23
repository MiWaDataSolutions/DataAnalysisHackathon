using DataAnalystBackend.Shared.AgentAPIModels;
using DataAnalystBackend.Shared.Exceptions;
using DataAnalystBackend.Shared.Interfaces;
using DataAnalystBackend.Shared.MessagingProviders.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public class DataSessionDataProcessMessageQueueHandler : IMessageQueueHandler
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

            Console.WriteLine($"Initializing {nameof(DataSessionDataProcessMessageQueueHandler)}");
            _connection = await connectionFactory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(queue: $"{_prefix}-{IMessagingProvider.DATA_SESSION_DATA_PROCESS}", durable: false, exclusive: false, autoDelete: false,
                arguments: null);


            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += Consume;

            Console.WriteLine($"Initialized {nameof(DataSessionDataProcessMessageQueueHandler)}");
        }

        private async Task Consume(object sender, BasicDeliverEventArgs ea)
        {
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
            agentClient.Timeout = TimeSpan.Parse("24.20:31:23.6470000");
            var response = await agentClient.PostAsync("/api/agent/start-data-session-processing", stringContent);

            if (response.IsSuccessStatusCode)
            {
                //string dataSessionNameJson = await response.Content.ReadAsStringAsync();

                //GetSessionNameModel dataSessionName = JsonSerializer.Deserialize<GetSessionNameModel>(dataSessionNameJson);

                //GenerateNameResponseMessage generateNameResponseMessage = new GenerateNameResponseMessage()
                //{
                //    DataSessionName = dataSessionName.DataSessionName
                //};

                string json = JsonSerializer.Serialize("true");

                var responseBytes = Encoding.UTF8.GetBytes(json);
                await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
                    mandatory: true, basicProperties: replyProps, body: responseBytes);
                await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        }

        public async Task StartConsuming()
        {
            Console.WriteLine($"Starting Consuming on {nameof(DataSessionDataProcessMessageQueueHandler)}");
            if (_consumer == null)
                throw new MessageProviderException($"{IMessagingProvider.DATA_SESSION_DATA_PROCESS} consumer not initialized");

            if (_channel == null)
                throw new MessageProviderException($"{IMessagingProvider.DATA_SESSION_DATA_PROCESS} channel not initialized");

            await _channel.BasicConsumeAsync($"{_prefix}-{IMessagingProvider.DATA_SESSION_DATA_PROCESS}", autoAck: true, consumer: _consumer);
        }
    }
}
