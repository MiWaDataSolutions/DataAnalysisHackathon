namespace DataAnalystBackend.Shared.DataAccess.Models
{
    public class DataSession
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? SchemaName
        {
            get
            {
                return $"ds_{Id.ToString().ToLower().Replace('-', '_')}";
            }
        }

        public DateTime CreatedAt { get; set; }

        public DateTime LastUpdatedAt { get; set; }

        public string UserId { get; set; }

        public User User { get; set; }

        public bool InitialFileHasHeaders { get; set; }
    }
}
