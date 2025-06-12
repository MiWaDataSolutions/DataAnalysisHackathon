using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DataAnalysisHackathonBackend.Models;
using System.Threading.Tasks;

namespace DataAnalysisHackathonBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;

        public UsersController(ILogger<UsersController> logger)
        {
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            if (user == null)
            {
                return BadRequest("User data is null.");
            }

            // Basic validation based on model annotations
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation($"Received user login attempt: GoogleId: {{GoogleId}}, Email: {{Email}}, Name: {{Name}}", user.GoogleId, user.Email, user.Name);

            // In a real application, you would typically:
            // 1. Validate the Google ID token (if sent from frontend) to ensure authenticity.
            // 2. Check if the user exists in your database.
            // 3. If not, create a new user record.
            // 4. If exists, update any necessary information (e.g., last login time, profile picture).
            // 5. Generate a session token or JWT for the backend session if needed.

            // For this subtask, we just acknowledge receipt and return the user data.
            return Ok(new { Message = "Login data received successfully.", User = user });
        }
    }
}
