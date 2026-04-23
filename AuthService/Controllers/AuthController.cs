using AuthService.DTOs;
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

        // POST /api/auth/register - create a new account
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // ModelState checks the [Required], [EmailAddress] etc. annotations on the DTO
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, data) = await _authService.RegisterAsync(dto);

            if (!success)
                return Conflict(new { message }); // 409 - resource already exists

            return CreatedAtAction(nameof(Register), new { id = data!.Id }, new
            {
                message,
                user = data
            });
        }

        // POST /api/auth/login - sign in with email + password
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, data) = await _authService.LoginAsync(dto);

            if (!success)
                return Unauthorized(new { message }); // 401 - credentials wrong

            return Ok(new { message, user = data });
        }
    }
}
