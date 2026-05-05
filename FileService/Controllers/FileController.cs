using System.Security.Claims;
using FileService.DTOs;
using FileService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers
{
    [ApiController]
    [Route("api/files")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FileController(IFileService fileService) => _fileService = fileService;

        private int GetUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // POST /api/files/file
        [HttpPost("file")]
        public async Task<IActionResult> CreateFile([FromBody] CreateFileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, message, data) = await _fileService.CreateFileAsync(GetUserId(), dto);
            if (!success) return BadRequest(new { message });
            return CreatedAtAction(nameof(GetById), new { id = data!.Id }, new { message, file = data });
        }

        // POST /api/files/folder
        [HttpPost("folder")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, message, data) = await _fileService.CreateFolderAsync(GetUserId(), dto);
            if (!success) return BadRequest(new { message });
            return CreatedAtAction(nameof(GetById), new { id = data!.Id }, new { message, folder = data });
        }

        // GET /api/files/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var (success, message, data) = await _fileService.GetByIdAsync(id, GetUserId());
            if (!success) return NotFound(new { message });
            return Ok(new { message, file = data });
        }

        // GET /api/files/tree/{projectId}
        [HttpGet("tree/{projectId:int}")]
        public async Task<IActionResult> GetTree(int projectId)
        {
            var (success, message, data) = await _fileService.GetFileTreeAsync(projectId, GetUserId());
            if (!success) return NotFound(new { message });
            return Ok(new { message, tree = data });
        }

        // PUT /api/files/{id}/content
        [HttpPut("{id:int}/content")]
        public async Task<IActionResult> UpdateContent(int id, [FromBody] UpdateCodeFileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, message, data) = await _fileService.UpdateContentAsync(id, GetUserId(), dto);
            if (!success)
            {
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { message });
                return StatusCode(403, new { message });
            }
            return Ok(new { message, file = data });
        }

        // DELETE /api/files/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _fileService.DeleteAsync(id, GetUserId());
            if (!success)
            {
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { message });
                return StatusCode(403, new { message });
            }
            return Ok(new { message });
        }
    }
}
