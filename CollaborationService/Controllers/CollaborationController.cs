using System.Security.Claims;
using CollaborationService.DTOs;
using CollaborationService.Models;
using CollaborationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollaborationService.Controllers
{
    [ApiController]
    [Route("api/collaboration")]
    [Authorize]
    public class CollaborationController : ControllerBase
    {
        private readonly ISessionStore _sessions;

        public CollaborationController(ISessionStore sessions) => _sessions = sessions;

        private int GetUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var session = await _sessions.CreateAsync(dto.FileId, dto.ProjectId, GetUserId());
            return Ok(new { message = "Session created.", session = MapToDto(session) });
        }

        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetSession(string sessionId)
        {
            var session = await _sessions.GetAsync(sessionId);
            if (session is null) return NotFound(new { message = "Session not found." });
            return Ok(new { message = "Session retrieved.", session = MapToDto(session) });
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetAllSessions()
        {
            var all = await _sessions.GetAllAsync();
            return Ok(new { message = "Sessions retrieved.", sessions = all.Select(MapToDto) });
        }

        private static SessionResponseDto MapToDto(CollaborationSession s) => new()
        {
            SessionId    = s.SessionId,
            FileId       = s.FileId,
            ProjectId    = s.ProjectId,
            OwnerId      = s.OwnerId,
            CreatedAt    = s.CreatedAt,
            Participants = s.JoinedUserIds
        };
    }
}
