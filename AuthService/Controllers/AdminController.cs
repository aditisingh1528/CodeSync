using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AdminController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET /api/admin/dashboard
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return Ok(new
            {
                message = "Welcome to the Admin Dashboard!",
                hint    = "Only users with Role = 'Admin' can see this.",
                tip     = "Use POST /api/admin/update-role to promote or demote users."
            });
        }

        // GET /api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(new
            {
                message = "All users retrieved successfully.",
                count   = users.Count(),
                users   // each item is UserSummaryDto — no PasswordHash!
            });
        }

        // POST /api/admin/update-role
        [HttpPost("update-role")]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message) = await _authService.UpdateUserRoleAsync(dto);

            if (!success)
                return NotFound(new { message });

            return Ok(new { message });
        }
    }
}
