using System.ComponentModel.DataAnnotations;

namespace DataAnalysisHackathonBackend.Models
{
    public class User
    {
        [Required]
        public string GoogleId { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string ProfilePictureUrl { get; set; } = string.Empty;
    }
}
