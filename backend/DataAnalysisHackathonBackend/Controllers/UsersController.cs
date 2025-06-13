using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DataAnalysisHackathonBackend.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

using DataAnalysisHackathonBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using DataAnalysisHackathonBackend.DTOs; // Required for StatusCodes

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

        public UsersController(ILogger<UsersController> logger) // Injected DbContext
        {
            _logger = logger;
        }

        [HttpGet("me")]
        [Authorize] // Ensures only authenticated (via cookie) users can access
        public ActionResult<MeDto> GetMe()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var googleId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email);
                var name = User.FindFirstValue(ClaimTypes.Name);
                var picture = User.FindFirstValue("urn:google:picture") ?? User.FindFirstValue("picture");

                _logger.LogInformation("User {GoogleId} requested their details via /me endpoint.", googleId);

                return Ok(new MeDto() 
                {
                    IsAuthenticated = true,
                    User = new MeUserDto() 
                    {
                        GoogleId = googleId,
                        Email = email,
                        Name = name,
                        ProfilePictureUrl = picture
                    }
                });
            }

            // This part should ideally not be reached if [Authorize] is effective and
            // the default challenge scheme redirects to login or returns 401.
            // However, explicitly returning Unauthorized if User.Identity is somehow not authenticated.
            _logger.LogWarning("/me endpoint reached by unauthenticated user despite [Authorize] attribute.");
            return Unauthorized(new { IsAuthenticated = false });
        }
    }
}
