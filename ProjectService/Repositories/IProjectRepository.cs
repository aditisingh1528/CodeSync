using ProjectService.Models;

namespace ProjectService.Repositories
{
    public interface IProjectRepository
    {
        // Get all projects owned by a specific user
        Task<IEnumerable<Project>> GetAllByUserIdAsync(int userId);

        // Get a single project by its Id
        Task<Project?> GetByIdAsync(int projectId);

        // Create a new project
        Task<Project> CreateAsync(Project project);

        // Update an existing project
        Task<Project> UpdateAsync(Project project);

        // Delete a project by Id
        Task<bool> DeleteAsync(int projectId);
    }
}
