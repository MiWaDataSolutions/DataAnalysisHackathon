using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DataAnalysisHackathonBackend.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DataAnalysisHackathonBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Protect all actions in this controller
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;

        public UsersController(ILogger<UsersController> logger)
        {
            _logger = logger;
        }

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

            // In a real application, you would:
            // 1. Query your database using 'googleId' from the token.
            // 2. If user exists, update Name (userRequestBody.Name) and ProfilePictureUrl (userRequestBody.ProfilePictureUrl) if provided.
            // 3. If user does not exist, create a new user record with googleId, email (from token),
            //    and Name/ProfilePictureUrl from userRequestBody.
            // 4. Return appropriate response (e.g., User details, session token if using separate backend sessions).

            return Ok(new { Message = "Login data processed successfully with token authorization.", User = responseUser });
        }
    }
}
