namespace DataAnalystBackend.DTOs
{
    public class MeDto
    {
        /// <summary>
        /// Whether the user is authenticate or not
        /// </summary>
        public bool IsAuthenticated {  get; set; }

        /// <summary>
        /// User details
        /// </summary>
        public MeUserDto? User { get; set; }
    }
}
