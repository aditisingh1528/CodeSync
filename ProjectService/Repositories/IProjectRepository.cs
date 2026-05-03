using ProjectService.Models;

namespace ProjectService.Repositories
{
    /// <summary>
    /// IPROJECTREPOSITORY
    /// ===================
    /// Defines all database operations for Projects.
    /// The controller and service layer depend on this interface,
    /// NOT on the concrete class — this makes unit testing easy
    /// (we can mock this interface in NUnit tests).
    /// </summary>
    public interface IProjectRepository
    {
        // Get all projects owned by a specific user
        Task<IEnumerable<Project>> GetAllByUserIdAsync(int userId);

        // Get a single project by its Id
        Task<Project?> GetByIdAsync(int projectId);

        // Create a new project — returns the saved entity (with Id assigned)
        Task<Project> CreateAsync(Project project);

        // Update an existing project — returns the updated entity
        Task<Project> UpdateAsync(Project project);

        // Delete a project by Id — returns true if deleted, false if not found
        Task<bool> DeleteAsync(int projectId);
    }
}
