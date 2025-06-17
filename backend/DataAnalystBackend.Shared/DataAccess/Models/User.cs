using System.ComponentModel.DataAnnotations;

namespace DataAnalystBackend.Shared.DataAccess.Models
{
    /// <summary>
    /// Represents a user's profile information, typically received from the client.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the user's unique Google identifier.
        /// This is primarily validated from the token claims on the backend and serves as the Primary Key.
        /// </summary>
        [Key] // Designate GoogleId as the primary key
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

        /// <summary>
        /// Gets or sets the UTC date and time when this user record was created in the database.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }


        /// <summary>
        /// Gets or sets the UTC date and time when this user record was updated in the database
        /// </summary>
        public DateTime LastLoginAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the users database conenction string
        /// </summary>
        public string? UserDatabaseConnectionString { get; set; }
    }
}
