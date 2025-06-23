using DataAnalystBackend.Shared.DataAccess.Enums;
using DataAnalystBackend.Shared.DataAccess.Models;

namespace DataAnalystBackend.DTOs
{
    public class DataSessionDTO
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? SchemaName
        {
            get; set;
        }

        public DateTime CreatedAt { get; set; }

        public DateTime LastUpdatedAt { get; set; }

        public string UserId { get; set; }

        public bool InitialFileHasHeaders { get; set; }

        public DataFileSessionStatus ProcessedStatus { get; set; }
    }
}
