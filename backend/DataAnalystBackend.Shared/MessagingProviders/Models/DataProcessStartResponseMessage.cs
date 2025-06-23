using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.MessagingProviders.Models
{
    public class DataProcessStartResponseMessage
    {
        [JsonPropertyName("processed")]
        public bool Processed { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("dataSessionId")]
        public Guid DataSessionId { get; set; }
    }
}
