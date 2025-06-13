using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies; // Added for CookieAuthenticationDefaults
using Microsoft.AspNetCore.Authorization; // Added for [Authorize] attribute
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims; // Added for ILogger

namespace DataAnalysisHackathonBackend.Controllers
{
    [Route("auth")] // Base route for auth actions
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger) // Added constructor for logger injection
        {
            _logger = logger;
        }

        [HttpGet("google-login")] // Consistent with LoginPath in Program.cs
        public IActionResult GoogleLogin([FromQuery] string redirectUrl)
        {
            // Define properties for the challenge.
            // The RedirectUri is where your application should redirect the user
            // AFTER they have successfully authenticated with Google AND your backend
            // has processed the /signin-google callback and established a session cookie.
            // This should typically be a frontend route.
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            // Challenge the GoogleDefaults.AuthenticationScheme.
            // This will trigger a redirect to Google's login page.
            // ASP.NET Core middleware will handle the actual redirect.
            return new ChallengeResult(GoogleDefaults.AuthenticationScheme, properties);
        }

        [HttpPost("logout")]
        [Authorize] // Ensures only authenticated users can attempt to logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User {UserId} logged out successfully at {Time}.",
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown", // Example of logging user ID
                DateTime.UtcNow);
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
