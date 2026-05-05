using ProjectService.DTOs;
using ProjectService.Models;
using ProjectService.Repositories;

namespace ProjectService.Services
{
    /// <summary>
    /// PROJECTSERVICEIMPL — WITH REDIS CACHING (UC-6)
    /// ================================================
    /// Business logic + cache-aside pattern for all project operations.
    ///
    /// CACHE-ASIDE PATTERN:
    ///   Read  → check cache first → if miss, read DB → store in cache → return
    ///   Write → write to DB → invalidate relevant cache keys → done
    ///
    /// WHY INVALIDATE (not update) on write?
    ///   Updating the cache on write is tricky — race conditions, partial updates.
    ///   Simpler and safer to just DELETE the stale cache entry.
    ///   Next read will be a cache miss → fetch fresh from DB → re-populate cache.
    ///
    /// CACHE KEY NAMING CONVENTION:
    ///   project:user:{userId}:all    → GetAll list for a user
    ///   project:user:{userId}:{id}  → Single project by Id
    ///
    ///   The "project:user:{userId}:" prefix ties all keys to a specific user.
    ///   On any write (create/update/delete), we:
    ///     1. Delete the specific project key  (project:user:{userId}:{id})
    ///     2. Delete all list keys for user    (project:user:{userId}:*)
    ///
    /// SECURITY RULE (unchanged from UC-5):
    ///   Every operation checks project.UserId == userId from JWT.
    ///   A user cannot read or modify another user's project.
    /// </summary>
    public class ProjectServiceImpl : IProjectService
    {
        private readonly IProjectRepository _repo;
        private readonly ICacheService      _cache;

        // ── Key Helpers ────────────────────────────────────────────────────
        // Centralised key building — if you rename the convention, change it here only.
        private static string AllKey(int userId)          => $"project:user:{userId}:all";
        private static string ByIdKey(int userId, int id) => $"project:user:{userId}:{id}";
        private static string UserPrefix(int userId)      => $"project:user:{userId}:";

        public ProjectServiceImpl(IProjectRepository repo, ICacheService cache)
        {
            _repo  = repo;
            _cache = cache;
        }

        // ─────────────────────────────────────────────────────────────────
        // CREATE
        // Write to DB → invalidate the "all" list for this user
        // ─────────────────────────────────────────────────────────────────
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

            // Invalidate the user's "all projects" list — it's now stale
            await _cache.RemoveByPrefixAsync(UserPrefix(userId));

            return (true, "Project created successfully.", MapToDto(created));
        }

        // ─────────────────────────────────────────────────────────────────
        // GET ALL (for this user only)
        // Cache-aside: check cache → miss → DB → store in cache
        // ─────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, IEnumerable<ProjectResponseDto>? Data)>
            GetAllByUserAsync(int userId)
        {
            var cacheKey = AllKey(userId);

            // 1. Try cache first
            var cached = await _cache.GetAsync<List<ProjectResponseDto>>(cacheKey);
            if (cached is not null)
                return (true, "Projects retrieved successfully. [cache]", cached);

            // 2. Cache miss → fetch from DB
            var projects = await _repo.GetAllByUserIdAsync(userId);
            var dtos     = projects.Select(MapToDto).ToList();

            // 3. Store in cache for next request
            await _cache.SetAsync(cacheKey, dtos);

            return (true, "Projects retrieved successfully.", dtos);
        }

        // ─────────────────────────────────────────────────────────────────
        // GET BY ID
        // Cache-aside: check cache → miss → DB → ownership check → store in cache
        // ─────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, ProjectResponseDto? Data)>
            GetByIdAsync(int projectId, int userId)
        {
            var cacheKey = ByIdKey(userId, projectId);

            // 1. Try cache first
            var cached = await _cache.GetAsync<ProjectResponseDto>(cacheKey);
            if (cached is not null)
                return (true, "Project retrieved successfully. [cache]", cached);

            // 2. Cache miss → fetch from DB
            var project = await _repo.GetByIdAsync(projectId);

            if (project is null)
                return (false, "Project not found.", null);

            // OWNERSHIP CHECK — cannot see someone else's project
            if (project.UserId != userId)
                return (false, "You do not have access to this project.", null);

            // 3. Store in cache
            var dto = MapToDto(project);
            await _cache.SetAsync(cacheKey, dto);

            return (true, "Project retrieved successfully.", dto);
        }

        // ─────────────────────────────────────────────────────────────────
        // UPDATE
        // Write to DB → invalidate both the specific key AND the list key
        // ─────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message, ProjectResponseDto? Data)>
            UpdateAsync(int projectId, int userId, UpdateProjectDto dto)
        {
            var project = await _repo.GetByIdAsync(projectId);

            if (project is null)
                return (false, "Project not found.", null);

            // OWNERSHIP CHECK
            if (project.UserId != userId)
                return (false, "You do not have access to this project.", null);

            project.Name        = dto.Name.Trim();
            project.Description = dto.Description.Trim();
            project.UpdatedAt   = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(project);

            // Invalidate: both the specific cached project AND the user's list
            await _cache.RemoveAsync(ByIdKey(userId, projectId));
            await _cache.RemoveByPrefixAsync(UserPrefix(userId));

            return (true, "Project updated successfully.", MapToDto(updated));
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE
        // Write to DB → invalidate both the specific key AND the list key
        // ─────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string Message)>
            DeleteAsync(int projectId, int userId)
        {
            var project = await _repo.GetByIdAsync(projectId);

            if (project is null)
                return (false, "Project not found.");

            // OWNERSHIP CHECK
            if (project.UserId != userId)
                return (false, "You do not have access to this project.");

            await _repo.DeleteAsync(projectId);

            // Invalidate: remove deleted project from cache + stale list
            await _cache.RemoveAsync(ByIdKey(userId, projectId));
            await _cache.RemoveByPrefixAsync(UserPrefix(userId));

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
