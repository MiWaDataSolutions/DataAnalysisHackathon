using DataAnalystBackend.Shared.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace DataAnalystBackend.Shared.Services.RPC
{
    public class RpcClient : IAsyncDisposable
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper
            = new();

        private IConnection? _connection;
        private IChannel? _channel;
        private string? _replyQueueName;
        private readonly IServiceProvider _serviceProvider;

        public RpcClient(IConfiguration config, IServiceProvider serviceProvider)
        {
            _connectionFactory= new ConnectionFactory { HostName = config.GetValue<string>("RabbitMQ:HostName") };
            string? username = config.GetValue<string>("RabbitMQ:Username");
            string? password = config.GetValue<string>("RabbitMQ:Password");
            if (!string.IsNullOrWhiteSpace(username))
                _connectionFactory.UserName = username;

            if (!string.IsNullOrWhiteSpace(password))
                _connectionFactory.Password = password;

            _serviceProvider = serviceProvider;

            Task.WaitAll(InitializeRPCClient());
        }

        private async Task InitializeRPCClient()
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
        }

        public async Task StartAsync<TModel>(Func<TModel, BasicDeliverEventArgs, IServiceProvider, Task> consumeMethod)
        {
            // declare a server-named queue
            QueueDeclareOk queueDeclareResult = await _channel.QueueDeclareAsync();
            _replyQueueName = queueDeclareResult.QueueName;
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += (model, ea) =>
            {
                string? correlationId = ea.BasicProperties.CorrelationId;

                if (!string.IsNullOrEmpty(correlationId))
                {
                    if (_callbackMapper.TryRemove(correlationId, out var tcs))
                    {
                        var body = ea.Body.ToArray();
                        var response = Encoding.UTF8.GetString(body);
                        tcs.TrySetResult(response);

                        TModel deserializedModel = JsonSerializer.Deserialize<TModel>(response);
                        return consumeMethod(deserializedModel, ea, _serviceProvider);
                    }
                }

                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(_replyQueueName, true, consumer);
        }

        public async Task<string> CallAsync<TInput>(TInput message, string destinationQueue,
            CancellationToken cancellationToken = default)
        {
            if (_channel is null)
            {
                throw new InvalidOperationException();
            }

            string correlationId = Guid.NewGuid().ToString();
            var props = new BasicProperties
            {
                CorrelationId = correlationId,
                ReplyTo = _replyQueueName
            };

            var tcs = new TaskCompletionSource<string>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            _callbackMapper.TryAdd(correlationId, tcs);

            string json = JsonSerializer.Serialize(message);

            var messageBytes = Encoding.UTF8.GetBytes(json);
            await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: destinationQueue,
                mandatory: true, basicProperties: props, body: messageBytes);

            using CancellationTokenRegistration ctr =
                cancellationToken.Register(() =>
                {
                    _callbackMapper.TryRemove(correlationId, out _);
                    tcs.SetCanceled();
                });

            return await tcs.Task;
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
            }

            if (_connection is not null)
            {
                await _connection.CloseAsync();
            }
        }
    }
}
