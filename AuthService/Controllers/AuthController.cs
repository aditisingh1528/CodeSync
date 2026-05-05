using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET /api/auth/test
        [HttpGet("test")]
        [AllowAnonymous]
        public async Task<IActionResult> Test()
        {
            var status = await _authService.GetServiceStatusAsync();
            return Ok(new { message = status, timestamp = DateTime.UtcNow });
        }

        // POST /api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, data) = await _authService.RegisterAsync(dto);

            if (!success)
                return Conflict(new { message });   // 409 - email/username taken

            return CreatedAtAction(nameof(Register), new { id = data!.Id }, new
            {
                message,
                user = data     // data.Token holds the JWT
            });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, data) = await _authService.LoginAsync(dto);

            if (!success)
                return Unauthorized(new { message }); 

            return Ok(new { message, user = data }); 
        }
    }
}
