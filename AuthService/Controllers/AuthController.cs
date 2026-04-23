using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    // controller handles incoming HTTP requests and sends responses
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET /api/auth/test - to check if the service is up
        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            var status = await _authService.GetServiceStatusAsync();
            return Ok(new { message = status, timestamp = DateTime.UtcNow });
        }

        // GET /api/auth/users - get all users from DB
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }
    }
}
