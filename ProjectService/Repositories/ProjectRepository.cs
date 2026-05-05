using Microsoft.EntityFrameworkCore;
using ProjectService.Data;
using ProjectService.Models;

namespace ProjectService.Repositories
{
   
    public class ProjectRepository : IProjectRepository
    {
        private readonly ProjectDbContext _db;

        public ProjectRepository(ProjectDbContext db)
        {
            _db = db;
        }

        // GET ALL PROJECTS FOR A USER
        public async Task<IEnumerable<Project>> GetAllByUserIdAsync(int userId)
        {
            return await _db.Projects
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.UpdatedAt) 
                .ToListAsync();
        }

        // GET SINGLE PROJECT BY ID
        public async Task<Project?> GetByIdAsync(int projectId)
        {
            return await _db.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        // CREATE PROJECT
        public async Task<Project> CreateAsync(Project project)
        {
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();
            return project; 
        }

        // UPDATE PROJECT
        public async Task<Project> UpdateAsync(Project project)
        {
            _db.Projects.Update(project);
            await _db.SaveChangesAsync();
            return project;
        }

        // DELETE PROJECT
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
