using DataAnalystBackend.Shared.MessagingProviders.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAnalystBackend.Shared.MessagingProviders.Models
{
    public class Message
    {
        public MessageType MessageType { get; set; }
    }

    public class Message<TData>
    {
        public MessageType MessageType { get; set; }

        public TData Data { get; set; }
    }
}
