using ProjectService.DTOs;

namespace ProjectService.Services
{
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
