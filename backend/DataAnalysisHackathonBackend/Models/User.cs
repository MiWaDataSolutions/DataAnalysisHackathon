using System.ComponentModel.DataAnnotations;

namespace DataAnalysisHackathonBackend.Models
{
    /// <summary>
    /// Represents a user's profile information, typically received from the client.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the user's unique Google identifier.
        /// This is primarily validated from the token claims on the backend.
        /// </summary>
        [Required]
        public string GoogleId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's email address.
        /// This is primarily validated from the token claims on the backend.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's display name.
        /// This is typically provided in the request body.
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL of the user's profile picture. Optional.
        /// </summary>
        public string ProfilePictureUrl { get; set; } = string.Empty;
    }
}
