using DataAnalystBackend.Shared.MessagingProviders.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.Interfaces
{
    public interface IMessagingProvider
    {
        // Queue names
        const string DATA_SESSION_GENERATE_NAME = "data_session_generate_name";
        const string DATA_SESSION_START = "data_session_start_session";
        const string DATA_SESSION_DATA_PROCESS = "data_session_process_data";

        Task PublishMessageAsync<TMessage>(Message<TMessage> message);
        Task PublishMessageAsync(Message message);
    }
}
