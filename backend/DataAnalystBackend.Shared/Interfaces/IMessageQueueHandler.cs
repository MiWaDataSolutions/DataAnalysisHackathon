using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.Interfaces
{
    public interface IMessageQueueHandler: IDisposable
    {
        Task InitializeQueue(ConnectionFactory connectionFactory, IServiceProvider hostProvider, IConfiguration configuration);

        Task StartConsuming();
    }
}
