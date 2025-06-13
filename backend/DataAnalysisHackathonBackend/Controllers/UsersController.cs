using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DataAnalysisHackathonBackend.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

using DataAnalysisHackathonBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // Required for StatusCodes

namespace DataAnalysisHackathonBackend.Controllers
{
    /// <summary>
    /// Manages user login and processes user-related information using ApplicationDbContext.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Protect all actions in this controller
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly ApplicationDbContext _context; // Added DbContext field

        public UsersController(ILogger<UsersController> logger, ApplicationDbContext context) // Injected DbContext
        {
            _logger = logger;
            _context = context; // Assign injected context
        }

        /// <summary>
        /// Processes user login using Google ID token, persists user data to database, and returns user details.
        /// Retrieves user identity from the token and additional profile details from the request body.
        /// Determines if it's the user's first login by checking the database.
        /// </summary>
        /// <param name="userRequestBody">User profile details like Name and ProfilePictureUrl.
        /// GoogleId and Email from this body are secondary to token claims.</param>
        /// <returns>An object containing login status, user details, and a flag indicating if it's a first login.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User userRequestBody)
        {
            var googleId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("User identifier (GoogleId) or Email not found in token.");
                return BadRequest("User identifier or email not found in token.");
            }

            _logger.LogInformation("User login attempt via token: GoogleID: {GoogleId}, Email: {Email}", googleId, email);

            // Validate userRequestBody if necessary, though [ApiController] handles basic model validation.
            // For example, if Name is mandatory from body even if token is present.
            if (userRequestBody == null || string.IsNullOrEmpty(userRequestBody.Name))
            {
                 _logger.LogWarning("Request body is null or missing Name for GoogleID: {GoogleId}", googleId);
                 // Depending on requirements, you might return BadRequest here if Name is critical from body.
                 // For this example, we'll allow it but Name might be null in the DB if not provided.
            }

            bool isFirstLoginToApp;
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (existingUser == null)
            {
                isFirstLoginToApp = true;
                _logger.LogInformation("First-time login to application for GoogleID: {GoogleId}. Creating new user.", googleId);

                User newUser = new User
                {
                    GoogleId = googleId,
                    Email = email, // From token
                    Name = userRequestBody?.Name ?? "User", // Default name if not provided in body
                    ProfilePictureUrl = userRequestBody?.ProfilePictureUrl ?? string.Empty, // From request body
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.Users.Add(newUser);
            }
            else
            {
                isFirstLoginToApp = false;
                _logger.LogInformation("Returning user login to application for GoogleID: {GoogleId}. User found in DB.", googleId);
                // Optionally, update existingUser details here if needed, e.g., Name, ProfilePictureUrl, LastLoginAtUtc
                // existingUser.Name = userRequestBody?.Name ?? existingUser.Name;
                // existingUser.ProfilePictureUrl = userRequestBody?.ProfilePictureUrl ?? existingUser.ProfilePictureUrl;
                // _context.Users.Update(existingUser); // Mark as modified if changes are made
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving changes to the database for GoogleID: {GoogleId}.", googleId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while saving user data.");
            }

            var responseUser = new
            {
                GoogleId = googleId,
                Email = email,
                Name = userRequestBody?.Name ?? (existingUser?.Name ?? "User"), // Use name from body, then existing, then default
                ProfilePictureUrl = userRequestBody?.ProfilePictureUrl ?? (existingUser?.ProfilePictureUrl ?? string.Empty)
            };

            return Ok(new {
                Message = isFirstLoginToApp ? "First-time login successful. User created." : "Returning user login successful.",
                User = responseUser,
                IsFirstLoginToApp = isFirstLoginToApp
            });
        }
    }
}
