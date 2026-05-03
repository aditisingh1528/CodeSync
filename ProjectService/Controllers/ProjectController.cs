using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectService.DTOs;
using ProjectService.Services;

namespace ProjectService.Controllers
{
    /// <summary>
    /// PROJECTCONTROLLER
    /// ==================
    /// All endpoints require a valid JWT — [Authorize] on the class.
    ///
    /// UserId is NEVER taken from the request body or URL.
    /// It is ALWAYS extracted from the JWT token claims.
    /// This means a user cannot impersonate another user by
    /// passing a different userId in the request.
    ///
    /// Routes:
    ///   POST   /api/projects          → Create project
    ///   GET    /api/projects          → Get all my projects
    ///   GET    /api/projects/{id}     → Get single project
    ///   PUT    /api/projects/{id}     → Update project
    ///   DELETE /api/projects/{id}     → Delete project
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]                         // ALL endpoints require valid JWT
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPER: extract UserId from JWT claim
        // JWT has claim: ClaimTypes.NameIdentifier = user's Id (int)
        // This is set by AuthService's JwtService.GenerateToken()
        // ─────────────────────────────────────────────────────────────────
        private int GetUserIdFromToken()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(claim!);
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /api/projects
        // Create a new project for the logged-in user
        // ─────────────────────────────────────────────────────────────────
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

        // ─────────────────────────────────────────────────────────────────
        // GET /api/projects
        // Get ALL projects belonging to the logged-in user
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserIdFromToken();
            var (success, message, data) = await _projectService.GetAllByUserAsync(userId);

            return Ok(new { message, projects = data });
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /api/projects/{id}
        // Get a single project — only if it belongs to the logged-in user
        // ─────────────────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserIdFromToken();
            var (success, message, data) = await _projectService.GetByIdAsync(id, userId);

            if (!success)
            {
                // "not found" → 404, ownership denial → 403
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { message });
                return StatusCode(403, new { message });
            }

            return Ok(new { message, project = data });
        }

        // ─────────────────────────────────────────────────────────────────
        // PUT /api/projects/{id}
        // Update a project — only if it belongs to the logged-in user
        // ─────────────────────────────────────────────────────────────────
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

        // ─────────────────────────────────────────────────────────────────
        // DELETE /api/projects/{id}
        // Delete a project — only if it belongs to the logged-in user
        // ─────────────────────────────────────────────────────────────────
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
