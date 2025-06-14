namespace DataAnalysisHackathonBackend.Models
{
    public class DataSession
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? SchemaName
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                    return null;

                return Name.ToLower().Replace(' ', '_');
            }
        }

        public DateTime CreatedAt { get; set; }

        public DateTime LastUpdatedAt { get; set; }

        public string UserId { get; set; }

        public User User { get; set; }
    }
}
