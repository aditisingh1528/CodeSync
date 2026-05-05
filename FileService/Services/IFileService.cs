using FileService.DTOs;

namespace FileService.Services
{
    public interface IFileService
    {
        Task<(bool Success, string Message, CodeFileResponseDto? Data)>
            CreateFileAsync(int userId, CreateFileDto dto);

        Task<(bool Success, string Message, CodeFileResponseDto? Data)>
            CreateFolderAsync(int userId, CreateFolderDto dto);

        Task<(bool Success, string Message, CodeFileResponseDto? Data)>
            UpdateContentAsync(int fileId, int userId, UpdateCodeFileDto dto);

        Task<(bool Success, string Message)>
            DeleteAsync(int fileId, int userId);

        Task<(bool Success, string Message, List<FileTreeNodeDto>? Data)>
            GetFileTreeAsync(int projectId, int userId);

        Task<(bool Success, string Message, CodeFileResponseDto? Data)>
            GetByIdAsync(int fileId, int userId);
    }
}
