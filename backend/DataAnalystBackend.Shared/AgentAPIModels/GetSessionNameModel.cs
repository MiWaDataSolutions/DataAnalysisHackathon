using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.AgentAPIModels
{
    public class GetSessionNameModel
    {
        [JsonPropertyName("dataSessionName")]
        public string DataSessionName { get; set; }
    }
}
