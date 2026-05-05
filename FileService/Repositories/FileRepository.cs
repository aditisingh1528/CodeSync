using FileService.Data;
using FileService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileService.Repositories
{
    public class FileRepository : IFileRepository
    {
        private readonly FileDbContext _db;

        public FileRepository(FileDbContext db) => _db = db;

        public async Task<CodeFile> CreateAsync(CodeFile file)
        {
            _db.CodeFiles.Add(file);
            await _db.SaveChangesAsync();
            return file;
        }

        public async Task<CodeFile?> GetByIdAsync(int id)
            => await _db.CodeFiles.FindAsync(id);

        // Get ALL files/folders for a project (flat list).
        // The service layer builds the tree from this flat list.
        public async Task<List<CodeFile>> GetAllByProjectAsync(int projectId)
            => await _db.CodeFiles
                        .Where(f => f.ProjectId == projectId)
                        .OrderByDescending(f => f.IsFolder)   // folders first, then files
                        .ThenBy(f => f.Name)
                        .ToListAsync();

        public async Task<CodeFile> UpdateAsync(CodeFile file)
        {
            _db.CodeFiles.Update(file);
            await _db.SaveChangesAsync();
            return file;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var file = await _db.CodeFiles.FindAsync(id);
            if (file is null) return false;
            _db.CodeFiles.Remove(file);
            await _db.SaveChangesAsync();
            return true;
        }

        // Used before deleting a folder — we don't want to orphan children
        public async Task<bool> HasChildrenAsync(int folderId)
            => await _db.CodeFiles.AnyAsync(f => f.ParentFolderId == folderId);
    }
}
