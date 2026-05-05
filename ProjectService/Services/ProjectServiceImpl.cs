using ProjectService.DTOs;
using ProjectService.Models;
using ProjectService.Repositories;

namespace ProjectService.Services
{
    public class ProjectServiceImpl : IProjectService
    {
        private readonly IProjectRepository _repo;
        private readonly ICacheService      _cache;

        private static string AllKey(int userId)          => $"project:user:{userId}:all";
        private static string ByIdKey(int userId, int id) => $"project:user:{userId}:{id}";
        private static string UserPrefix(int userId)      => $"project:user:{userId}:";

        public ProjectServiceImpl(IProjectRepository repo, ICacheService cache)
        {
            _repo  = repo;
            _cache = cache;
        }

        public async Task<(bool Success, string Message, ProjectResponseDto? Data)>
            CreateAsync(int userId, CreateProjectDto dto)
        {
            var project = new Project
            {
                Name        = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                UserId      = userId,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };

            var created = await _repo.CreateAsync(project);

            await _cache.RemoveByPrefixAsync(UserPrefix(userId));

            return (true, "Project created successfully.", MapToDto(created));
        }

        
        public async Task<(bool Success, string Message, IEnumerable<ProjectResponseDto>? Data)>
            GetAllByUserAsync(int userId)
        {
            var cacheKey = AllKey(userId);

            var cached = await _cache.GetAsync<List<ProjectResponseDto>>(cacheKey);
            if (cached is not null)
                return (true, "Projects retrieved successfully. [cache]", cached);

            var projects = await _repo.GetAllByUserIdAsync(userId);
            var dtos     = projects.Select(MapToDto).ToList();

            await _cache.SetAsync(cacheKey, dtos);

            return (true, "Projects retrieved successfully.", dtos);
        }

        public async Task<(bool Success, string Message, ProjectResponseDto? Data)>
            GetByIdAsync(int projectId, int userId)
        {
            var cacheKey = ByIdKey(userId, projectId);

            var cached = await _cache.GetAsync<ProjectResponseDto>(cacheKey);
            if (cached is not null)
                return (true, "Project retrieved successfully. [cache]", cached);

            var project = await _repo.GetByIdAsync(projectId);

            if (project is null)
                return (false, "Project not found.", null);

            if (project.UserId != userId)
                return (false, "You do not have access to this project.", null);

            var dto = MapToDto(project);
            await _cache.SetAsync(cacheKey, dto);

            return (true, "Project retrieved successfully.", dto);
        }

        
        public async Task<(bool Success, string Message, ProjectResponseDto? Data)>
            UpdateAsync(int projectId, int userId, UpdateProjectDto dto)
        {
            var project = await _repo.GetByIdAsync(projectId);

            if (project is null)
                return (false, "Project not found.", null);

            if (project.UserId != userId)
                return (false, "You do not have access to this project.", null);

            project.Name        = dto.Name.Trim();
            project.Description = dto.Description.Trim();
            project.UpdatedAt   = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(project);

            await _cache.RemoveAsync(ByIdKey(userId, projectId));
            await _cache.RemoveByPrefixAsync(UserPrefix(userId));

            return (true, "Project updated successfully.", MapToDto(updated));
        }

        public async Task<(bool Success, string Message)>
            DeleteAsync(int projectId, int userId)
        {
            var project = await _repo.GetByIdAsync(projectId);

            if (project is null)
                return (false, "Project not found.");

            if (project.UserId != userId)
                return (false, "You do not have access to this project.");

            await _repo.DeleteAsync(projectId);

            await _cache.RemoveAsync(ByIdKey(userId, projectId));
            await _cache.RemoveByPrefixAsync(UserPrefix(userId));

            return (true, "Project deleted successfully.");
        }

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
