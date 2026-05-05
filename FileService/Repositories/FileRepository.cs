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

        public async Task<bool> HasChildrenAsync(int folderId)
            => await _db.CodeFiles.AnyAsync(f => f.ParentFolderId == folderId);
    }
}
