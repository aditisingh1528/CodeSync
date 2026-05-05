using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service) => _service = service;

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("my")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var (success, message, data) = await _service.GetMyNotificationsAsync(CurrentUserId);
            if (!success) return BadRequest(new { message });
            return Ok(new { message, data });
        }

        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var (success, message) = await _service.MarkAsReadAsync(id, CurrentUserId);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }
    }
}
