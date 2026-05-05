using FileService.Models;

namespace FileService.Repositories
{
    public interface IFileRepository
    {
        Task<CodeFile>             CreateAsync(CodeFile file);
        Task<CodeFile?>            GetByIdAsync(int id);
        Task<List<CodeFile>>       GetAllByProjectAsync(int projectId);
        Task<CodeFile>             UpdateAsync(CodeFile file);
        Task<bool>                 DeleteAsync(int id);
        Task<bool>                 HasChildrenAsync(int folderId);
    }
}
