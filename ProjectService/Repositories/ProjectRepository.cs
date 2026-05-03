using Microsoft.EntityFrameworkCore;
using ProjectService.Data;
using ProjectService.Models;

namespace ProjectService.Repositories
{
    /// <summary>
    /// PROJECTREPOSITORY
    /// ==================
    /// Concrete EF Core implementation of IProjectRepository.
    /// All database access goes through here — the service layer
    /// never touches DbContext directly.
    /// </summary>
    public class ProjectRepository : IProjectRepository
    {
        private readonly ProjectDbContext _db;

        public ProjectRepository(ProjectDbContext db)
        {
            _db = db;
        }

        // ─────────────────────────────────────────────────────────────────
        // GET ALL PROJECTS FOR A USER
        // Returns only THIS user's projects — never leaks other users' data
        // ─────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<Project>> GetAllByUserIdAsync(int userId)
        {
            return await _db.Projects
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.UpdatedAt)   // most recently updated first
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────────
        // GET SINGLE PROJECT BY ID
        // Returns null if not found — caller decides what to do
        // ─────────────────────────────────────────────────────────────────
        public async Task<Project?> GetByIdAsync(int projectId)
        {
            return await _db.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        // ─────────────────────────────────────────────────────────────────
        // CREATE PROJECT
        // CreatedAt and UpdatedAt are already set on the entity before
        // this is called (in the service layer)
        // ─────────────────────────────────────────────────────────────────
        public async Task<Project> CreateAsync(Project project)
        {
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();
            return project;   // EF Core populates project.Id after SaveChanges
        }

        // ─────────────────────────────────────────────────────────────────
        // UPDATE PROJECT
        // We only update Name, Description, UpdatedAt —
        // never touch UserId or CreatedAt
        // ─────────────────────────────────────────────────────────────────
        public async Task<Project> UpdateAsync(Project project)
        {
            _db.Projects.Update(project);
            await _db.SaveChangesAsync();
            return project;
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE PROJECT
        // Returns false if project not found (so controller can return 404)
        // ─────────────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int projectId)
        {
            var project = await _db.Projects.FindAsync(projectId);
            if (project == null) return false;

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
