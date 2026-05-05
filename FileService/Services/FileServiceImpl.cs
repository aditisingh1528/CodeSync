using FileService.DTOs;
using FileService.Models;
using FileService.Repositories;

namespace FileService.Services
{
    public class FileServiceImpl : IFileService
    {
        private readonly IFileRepository _repo;
        private readonly ICacheService   _cache;

        //  Cache key helpers 
        private static string ContentKey(int fileId)    => $"file:content:{fileId}";
        private static string TreeKey(int projectId)    => $"file:tree:{projectId}";

        public FileServiceImpl(IFileRepository repo, ICacheService cache)
        {
            _repo  = repo;
            _cache = cache;
        }

        //  CREATE FILE 
        public async Task<(bool Success, string Message, CodeFileResponseDto? Data)>
            CreateFileAsync(int userId, CreateFileDto dto)
        {
            var (isValidParent, parentMessage, parentFolderId) =
                await ValidateParentFolderAsync(dto.ParentFolderId, dto.ProjectId);
            if (!isValidParent) return (false, parentMessage, null);

            var file = new CodeFile
            {
                Name            = dto.Name.Trim(),
                Content         = dto.Content,
                ProjectId       = dto.ProjectId,
                ParentFolderId  = parentFolderId,
                IsFolder        = false,
                CreatedByUserId = userId,
                CreatedAt       = DateTime.UtcNow,
                UpdatedAt       = DateTime.UtcNow
            };

            var created = await _repo.CreateAsync(file);

            await _cache.RemoveAsync(TreeKey(dto.ProjectId));

            return (true, "File created successfully.", MapToDto(created));
        }

        //  CREATE FOLDER 
        public async Task<(bool Success, string Message, CodeFileResponseDto? Data)>
            CreateFolderAsync(int userId, CreateFolderDto dto)
        {
            var (isValidParent, parentMessage, parentFolderId) =
                await ValidateParentFolderAsync(dto.ParentFolderId, dto.ProjectId);
            if (!isValidParent) return (false, parentMessage, null);

            var folder = new CodeFile
            {
                Name            = dto.Name.Trim(),
                Content         = null,   // folders never have content
                ProjectId       = dto.ProjectId,
                ParentFolderId  = parentFolderId,
                IsFolder        = true,
                CreatedByUserId = userId,
                CreatedAt       = DateTime.UtcNow,
                UpdatedAt       = DateTime.UtcNow
            };

            var created = await _repo.CreateAsync(folder);

            await _cache.RemoveAsync(TreeKey(dto.ProjectId));

            return (true, "Folder created successfully.", MapToDto(created));
        }

        //  UPDATE CONTENT 
        public async Task<(bool Success, string Message, CodeFileResponseDto? Data)>
            UpdateContentAsync(int fileId, int userId, UpdateCodeFileDto dto)
        {
            var file = await _repo.GetByIdAsync(fileId);
            if (file is null) return (false, "File not found.", null);

            if (file.CreatedByUserId != userId)
                return (false, "You do not have access to this file.", null);

            if (file.IsFolder)
                return (false, "Cannot update content of a folder.", null);

            file.Content   = dto.Content;
            file.UpdatedAt = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(file);

            await _cache.RemoveAsync(ContentKey(fileId));
            await _cache.RemoveAsync(TreeKey(file.ProjectId));

            return (true, "File content updated.", MapToDto(updated));
        }

        //  DELETE 
        public async Task<(bool Success, string Message)>
            DeleteAsync(int fileId, int userId)
        {
            var file = await _repo.GetByIdAsync(fileId);
            if (file is null) return (false, "File not found.");

            if (file.CreatedByUserId != userId)
                return (false, "You do not have access to this file.");

            if (file.IsFolder && await _repo.HasChildrenAsync(fileId))
                return (false, "Cannot delete a folder that still has files inside. Delete the contents first.");

            var projectId = file.ProjectId;
            await _repo.DeleteAsync(fileId);

            await _cache.RemoveAsync(ContentKey(fileId));
            await _cache.RemoveAsync(TreeKey(projectId));

            return (true, $"{(file.IsFolder ? "Folder" : "File")} deleted successfully.");
        }

        //  GET FILE TREE 
        public async Task<(bool Success, string Message, List<FileTreeNodeDto>? Data)>
            GetFileTreeAsync(int projectId, int userId)
        {
            var cacheKey = TreeKey(projectId);

            var cached = await _cache.GetAsync<List<FileTreeNodeDto>>(cacheKey);
            if (cached is not null)
                return (true, "File tree retrieved. [cache]", cached);

            var allFiles = await _repo.GetAllByProjectAsync(projectId);

            if (!allFiles.Any())
                return (true, "No files found.", new List<FileTreeNodeDto>());

            var tree = BuildTree(allFiles, parentId: null);
            await _cache.SetAsync(cacheKey, tree);

            return (true, "File tree retrieved.", tree);
        }

        public async Task<(bool Success, string Message, CodeFileResponseDto? Data)>
            GetByIdAsync(int fileId, int userId)
        {
            var cacheKey = ContentKey(fileId);

            var cached = await _cache.GetAsync<CodeFileResponseDto>(cacheKey);
            if (cached is not null)
                return (true, "File retrieved. [cache]", cached);

            var file = await _repo.GetByIdAsync(fileId);
            if (file is null) return (false, "File not found.", null);

            var dto = MapToDto(file);
            await _cache.SetAsync(cacheKey, dto);

            return (true, "File retrieved.", dto);
        }

        //  TREE BUILDER
        private static List<FileTreeNodeDto> BuildTree(List<CodeFile> all, int? parentId)
        {
            return all
                .Where(f => f.ParentFolderId == parentId)
                .OrderByDescending(f => f.IsFolder)   // folders first
                .ThenBy(f => f.Name)
                .Select(f => new FileTreeNodeDto
                {
                    Id              = f.Id,
                    Name            = f.Name,
                    IsFolder        = f.IsFolder,
                    ParentFolderId  = f.ParentFolderId,
                    Content         = f.IsFolder ? null : f.Content,
                    CreatedByUserId = f.CreatedByUserId,
                    Children        = f.IsFolder
                                        ? BuildTree(all, f.Id)
                                        : new List<FileTreeNodeDto>()
                })
                .ToList();
        }

        private async Task<(bool Success, string Message, int? ParentFolderId)>
            ValidateParentFolderAsync(int? parentFolderId, int projectId)
        {
            if (!parentFolderId.HasValue || parentFolderId.Value == 0)
                return (true, string.Empty, null);

            if (parentFolderId.Value < 0)
                return (false, "Parent folder id is invalid.", null);

            var parent = await _repo.GetByIdAsync(parentFolderId.Value);
            if (parent is null)
                return (false, "Parent folder not found.", null);

            if (!parent.IsFolder)
                return (false, "Parent must be a folder.", null);

            if (parent.ProjectId != projectId)
                return (false, "Parent folder belongs to a different project.", null);

            return (true, string.Empty, parentFolderId);
        }

        //  MAPPER 
        private static CodeFileResponseDto MapToDto(CodeFile f) => new()
        {
            Id              = f.Id,
            Name            = f.Name,
            Content         = f.Content,
            ProjectId       = f.ProjectId,
            ParentFolderId  = f.ParentFolderId,
            IsFolder        = f.IsFolder,
            CreatedAt       = f.CreatedAt,
            UpdatedAt       = f.UpdatedAt,
            CreatedByUserId = f.CreatedByUserId
        };
    }
}
