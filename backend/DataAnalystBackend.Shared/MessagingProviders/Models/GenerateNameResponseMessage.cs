using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.MessagingProviders.Models
{
    public class GenerateNameResponseMessage
    {
        [JsonPropertyName("dataSessionName")]
        public string DataSessionName { get; set; }
    }
}
