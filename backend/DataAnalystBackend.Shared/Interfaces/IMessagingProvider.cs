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
        const string DATA_SESSION_NAME_GENERATED = "data_session_name_generated";

        Task PublishMessageAsync<TMessage>(Message<TMessage> message);
        Task PublishMessageAsync(Message message);
    }
}
