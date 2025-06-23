using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.MessagingProviders.Models
{
    public class GeneralAgentProcessingRequest
    {
        public string UserId { get; set; }

        public Guid DataSessionId { get; set; }
    }
}
