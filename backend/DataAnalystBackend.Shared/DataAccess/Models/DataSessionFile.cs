namespace DataAnalystBackend.Shared.DataAccess.Models
{
    public class DataSessionFile
    {
        public Guid Id { get; set; }

        public string Filename { get; set; }

        public byte[] FileData { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public Guid DataSessionId { get; set; }

        public DataSession DataSession { get; set; }

        public bool Processed { get; set; }
    }
}
