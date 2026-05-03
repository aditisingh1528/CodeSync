using ProjectService.DTOs;
using ProjectService.Models;
using ProjectService.Repositories;

namespace ProjectService.Services
{
    /// <summary>
    /// PROJECTSERVICEIMPL
    /// ===================
    /// Business logic for all project operations.
    ///
    /// KEY SECURITY RULE enforced here:
    ///   Every operation that reads/modifies a project checks that
    ///   project.UserId == userId (from JWT).
    ///   This prevents User A from reading or editing User B's projects.
    ///   This check is in the SERVICE layer, not the controller,
    ///   so it cannot be bypassed.
    /// </summary>
    public class ProjectServiceImpl : IProjectService
    {
        private readonly IProjectRepository _repo;

        public ProjectServiceImpl(IProjectRepository repo)
        {
            _repo = repo;
        }

        // ─────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, ProjectResponseDto? Data)>
            CreateAsync(int userId, CreateProjectDto dto)
        {
            var project = new Project
            {
                Name        = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                UserId      = userId,               // owner = logged-in user
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };

            var created = await _repo.CreateAsync(project);

            return (true, "Project created successfully.", MapToDto(created));
        }

        // ─────────────────────────────────────────────────────────────────
        // GET ALL (for this user only)
        // ─────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, IEnumerable<ProjectResponseDto>? Data)>
            GetAllByUserAsync(int userId)
        {
            var projects = await _repo.GetAllByUserIdAsync(userId);
            var dtos     = projects.Select(MapToDto);

            return (true, "Projects retrieved successfully.", dtos);
        }

        // ─────────────────────────────────────────────────────────────────
        // GET BY ID
        // Ownership check: only owner can view their project
        // ─────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, ProjectResponseDto? Data)>
            GetByIdAsync(int projectId, int userId)
        {
            var project = await _repo.GetByIdAsync(projectId);

            if (project == null)
                return (false, "Project not found.", null);

            // OWNERSHIP CHECK — cannot see someone else's project
            if (project.UserId != userId)
                return (false, "You do not have access to this project.", null);

            return (true, "Project retrieved successfully.", MapToDto(project));
        }

        // ─────────────────────────────────────────────────────────────────
        // UPDATE
        // Ownership check: only owner can update their project
        // ─────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, ProjectResponseDto? Data)>
            UpdateAsync(int projectId, int userId, UpdateProjectDto dto)
        {
            var project = await _repo.GetByIdAsync(projectId);

            if (project == null)
                return (false, "Project not found.", null);

            // OWNERSHIP CHECK — cannot edit someone else's project
            if (project.UserId != userId)
                return (false, "You do not have access to this project.", null);

            // Apply changes — never touch UserId or CreatedAt
            project.Name        = dto.Name.Trim();
            project.Description = dto.Description.Trim();
            project.UpdatedAt   = DateTime.UtcNow;   // bump the timestamp

            var updated = await _repo.UpdateAsync(project);

            return (true, "Project updated successfully.", MapToDto(updated));
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE
        // Ownership check: only owner can delete their project
        // ─────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message)>
            DeleteAsync(int projectId, int userId)
        {
            var project = await _repo.GetByIdAsync(projectId);

            if (project == null)
                return (false, "Project not found.");

            // OWNERSHIP CHECK — cannot delete someone else's project
            if (project.UserId != userId)
                return (false, "You do not have access to this project.");

            await _repo.DeleteAsync(projectId);

            return (true, "Project deleted successfully.");
        }

        // ─────────────────────────────────────────────────────────────────
        // PRIVATE HELPER — maps Project entity → ProjectResponseDto
        // ─────────────────────────────────────────────────────────────────
        private static ProjectResponseDto MapToDto(Project p) => new()
        {
            Id          = p.Id,
            Name        = p.Name,
            Description = p.Description,
            UserId      = p.UserId,
            CreatedAt   = p.CreatedAt,
            UpdatedAt   = p.UpdatedAt
        };
    }
}
