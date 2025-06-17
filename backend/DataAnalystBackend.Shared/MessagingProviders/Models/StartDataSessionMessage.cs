using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.MessagingProviders.Models
{
    public class StartDataSessionMessage
    {
        public Guid DataSessionId { get; set; }

        public string UserId { get; set; }

        public string UserConnString { get; set; }

        public string DataSessionSchema { get; set; }
    }
}
