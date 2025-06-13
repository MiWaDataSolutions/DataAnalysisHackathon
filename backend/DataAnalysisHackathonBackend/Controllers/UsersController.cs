using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DataAnalysisHackathonBackend.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DataAnalysisHackathonBackend.Controllers
{
    /// <summary>
    /// Manages user login and processes user-related information.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Protect all actions in this controller
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;

        // Placeholder for a database of seen Google IDs.
        // IMPORTANT: This is for demonstration purposes only and will reset with application restarts.
        // In a production environment, this would be replaced by a persistent database.
        private static HashSet<string> _seenGoogleIds = new HashSet<string>();

        public UsersController(ILogger<UsersController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Processes user login using Google ID token.
        /// Retrieves user identity from the token and additional profile details from the request body.
        /// Determines if it's the user's first login to the application.
        /// </summary>
        /// <param name="userRequestBody">User profile details like Name and ProfilePictureUrl.
        /// GoogleId and Email from this body are secondary to token claims.</param>
        /// <returns>An object containing login status, user details, and a flag indicating if it's a first login.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User userRequestBody) // Renamed to avoid confusion with User claims
        {
            var googleId = User.FindFirstValue(ClaimTypes.NameIdentifier); // 'sub' claim
            var email = User.FindFirstValue(ClaimTypes.Email);
            // var nameFromToken = User.FindFirstValue(ClaimTypes.Name); // Google often includes name in token

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("User identifier (GoogleId) or Email not found in token.");
                return BadRequest("User identifier or email not found in token.");
            }

            _logger.LogInformation("User login attempt via token: GoogleID: {GoogleId}, Email: {Email}", googleId, email);

            // The [ApiController] attribute automatically handles model validation for userRequestBody
            // if it's invalid (e.g., missing required fields if we were to make Name required in User model),
            // it would return a 400 Bad Request.
            // For this endpoint, we primarily trust the token for GoogleId and Email.
            // Name and ProfilePictureUrl are taken from the request body.

            if (userRequestBody == null)
            {
                 // This check might be redundant if [ApiController] and [FromBody] handle it,
                 // but can be kept for explicit clarity or if User model fields are optional.
                _logger.LogWarning("Request body (user data) was null for token-identified user GoogleID: {GoogleId}", googleId);
                // We might still proceed if only Name/ProfilePictureUrl are optional from body
                // For now, let's assume some body content (like name) is expected for profile completion.
                // If userRequestBody.Name is critical, add specific checks.
            }

            var responseUser = new
            {
                GoogleId = googleId, // From token
                Email = email,       // From token
                Name = userRequestBody?.Name, // From body, null-conditional for safety
                ProfilePictureUrl = userRequestBody?.ProfilePictureUrl // From body, null-conditional
            };

            _logger.LogInformation("Processed login for GoogleID: {GoogleId} with details from body: Name: {Name}", googleId, responseUser.Name);

            bool isFirstLoginToApp;
            lock (_seenGoogleIds)
            {
                if (!_seenGoogleIds.Contains(googleId))
                {
                    isFirstLoginToApp = true;
                    _seenGoogleIds.Add(googleId);
                    _logger.LogInformation("First-time login to application for GoogleID: {GoogleId}. Added to in-memory store.", googleId);
                    // In a real app, this is where you'd save the new user to the database.
                }
                else
                {
                    isFirstLoginToApp = false;
                    _logger.LogInformation("Returning user login to application for GoogleID: {GoogleId}. Found in in-memory store.", googleId);
                }
            }

            // In a real application, you would:
            // 1. Query your database using 'googleId' from the token. The 'isFirstLoginToApp' logic would be based on this DB query.
            // 2. If user exists (isFirstLoginToApp == false), update Name (userRequestBody.Name) and ProfilePictureUrl (userRequestBody.ProfilePictureUrl) if provided.
            // 3. If user does not exist (isFirstLoginToApp == true), create a new user record with googleId, email (from token),
            //    and Name/ProfilePictureUrl from userRequestBody.
            // 4. Return appropriate response.

            return Ok(new {
                Message = isFirstLoginToApp ? "First-time login successful." : "Returning user login successful.",
                User = responseUser,
                IsFirstLoginToApp = isFirstLoginToApp
            });
        }
    }
}
