namespace DataAnalystBackend.DTOs
{
    public class MeUserDto
    {
        /// <summary>
        /// The user's Google Id
        /// </summary>
        public string? GoogleId { get; set; }

        /// <summary>
        /// The user's email address
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// The user's name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The user's profile picture
        /// </summary>
        public string? ProfilePictureUrl { get; set; }
    }
}
