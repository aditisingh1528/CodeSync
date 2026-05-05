using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectService.DTOs;
using ProjectService.Services;

namespace ProjectService.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

       
        private int GetUserIdFromToken()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(claim!);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            var (success, message, data) = await _projectService.CreateAsync(userId, dto);

            if (!success) return BadRequest(new { message });

            return CreatedAtAction(nameof(GetById), new { id = data!.Id }, new { message, project = data });
        }

        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserIdFromToken();
            var (success, message, data) = await _projectService.GetAllByUserAsync(userId);

            return Ok(new { message, projects = data });
        }

        
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserIdFromToken();
            var (success, message, data) = await _projectService.GetByIdAsync(id, userId);

            if (!success)
            {
                
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { message });
                return StatusCode(403, new { message });
            }

            return Ok(new { message, project = data });
        }

        
        // PUT /api/projects/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            var (success, message, data) = await _projectService.UpdateAsync(id, userId, dto);

            if (!success)
            {
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { message });
                return StatusCode(403, new { message });
            }

            return Ok(new { message, project = data });
        }

        // DELETE /api/projects/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserIdFromToken();
            var (success, message) = await _projectService.DeleteAsync(id, userId);

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
