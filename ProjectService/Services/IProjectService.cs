using ProjectService.DTOs;

namespace ProjectService.Services
{
    /// <summary>
    /// IPROJECTSERVICE
    /// ================
    /// Business logic layer interface.
    /// The controller calls this — not the repository directly.
    ///
    /// Return type pattern:
    ///   (bool success, string message, T? data)
    ///   — same pattern used in AuthService for consistency
    /// </summary>
    public interface IProjectService
    {
        Task<(bool Success, string Message, ProjectResponseDto? Data)>
            CreateAsync(int userId, CreateProjectDto dto);

        Task<(bool Success, string Message, IEnumerable<ProjectResponseDto>? Data)>
            GetAllByUserAsync(int userId);

        Task<(bool Success, string Message, ProjectResponseDto? Data)>
            GetByIdAsync(int projectId, int userId);

        Task<(bool Success, string Message, ProjectResponseDto? Data)>
            UpdateAsync(int projectId, int userId, UpdateProjectDto dto);

        Task<(bool Success, string Message)>
            DeleteAsync(int projectId, int userId);
    }
}
