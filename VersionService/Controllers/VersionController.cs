using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersionService.DTOs;
using VersionService.Services;

namespace VersionService.Controllers
{
    [ApiController]
    [Route("api/versions")]
    [Authorize]
    public class VersionController : ControllerBase
    {
        private readonly IVersionService _versionService;

        public VersionController(IVersionService versionService) => _versionService = versionService;

        private int    GetUserId()      => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetBearerToken() => Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        [HttpPost("snapshot")]
        public async Task<IActionResult> CreateSnapshot([FromBody] CreateSnapshotDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, message, data) = await _versionService.CreateSnapshotAsync(GetUserId(), dto);
            if (!success) return BadRequest(new { message });
            return CreatedAtAction(nameof(GetHistory), new { fileId = data!.FileId }, new { message, snapshot = data });
        }

        [HttpGet("history/{fileId:int}")]
        public async Task<IActionResult> GetHistory(int fileId)
        {
            var (_, message, data) = await _versionService.GetFileHistoryAsync(fileId);
            return Ok(new { message, snapshots = data });
        }

        [HttpPost("restore/{snapshotId:int}")]
        public async Task<IActionResult> Restore(int snapshotId)
        {
            var (success, message, data) = await _versionService.RestoreSnapshotAsync(
                snapshotId, GetUserId(), GetBearerToken());

            if (!success)
            {
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { message });
                return StatusCode(503, new { message });
            }

            return Ok(new { message, snapshot = data });
        }
    }
}
