using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    /// <summary>
    /// SECURED endpoints — any logged-in user (valid JWT) can access these.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UserController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET /api/user/profile
        // Returns calling user's info read from JWT claims — no DB call needed
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var email    = User.FindFirstValue(ClaimTypes.Email);
            var role     = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new
            {
                message = "Profile retrieved successfully.",
                userId,
                username,
                email,
                role
            });
        }

        // GET /api/user/me
        // tokenIssuedAt reads the real "iat" claim from the token —
        // shows when token was CREATED, not when this endpoint was called
        [HttpGet("me")]
        public IActionResult Me()
        {
            var role     = User.FindFirstValue(ClaimTypes.Role);
            var iatClaim = User.FindFirstValue(JwtRegisteredClaimNames.Iat);

            DateTime? tokenIssuedAt = null;
            if (long.TryParse(iatClaim, out long iatSeconds))
                tokenIssuedAt = DateTimeOffset.FromUnixTimeSeconds(iatSeconds).UtcDateTime;

            return Ok(new
            {
                message       = $"Hello {User.FindFirstValue(ClaimTypes.Name)}!",
                yourUserId    = User.FindFirstValue(ClaimTypes.NameIdentifier),
                yourRole      = role,
                isAdmin       = role == "Admin",
                tokenIssuedAt
            });
        }
    }
}
