using VersionService.DTOs;
using VersionService.Models;
using VersionService.Repositories;

namespace VersionService.Services
{
    public interface IVersionService
    {
        Task<(bool Success, string Message, SnapshotResponseDto? Data)>
            CreateSnapshotAsync(int userId, CreateSnapshotDto dto);

        Task<(bool Success, string Message, List<SnapshotResponseDto> Data)>
            GetFileHistoryAsync(int fileId);

        Task<(bool Success, string Message, SnapshotResponseDto? Data)>
            RestoreSnapshotAsync(int snapshotId, int userId, string bearerToken);
    }

    public class VersionServiceImpl : IVersionService
    {
        private readonly ISnapshotRepository _repo;
        private readonly IFileServiceClient  _fileClient;

        public VersionServiceImpl(ISnapshotRepository repo, IFileServiceClient fileClient)
        {
            _repo       = repo;
            _fileClient = fileClient;
        }

        public async Task<(bool Success, string Message, SnapshotResponseDto? Data)>
            CreateSnapshotAsync(int userId, CreateSnapshotDto dto)
        {
            var snapshot = new Snapshot
            {
                FileId          = dto.FileId,
                Content         = dto.Content,
                Timestamp       = DateTime.UtcNow,
                CreatedByUserId = userId,
                Message         = dto.Message.Trim()
            };

            var created = await _repo.CreateAsync(snapshot);
            return (true, "Snapshot created.", MapToDto(created));
        }

        public async Task<(bool Success, string Message, List<SnapshotResponseDto> Data)>
            GetFileHistoryAsync(int fileId)
        {
            var snapshots = await _repo.GetByFileIdAsync(fileId);
            return (true, "File history retrieved.", snapshots.Select(MapToDto).ToList());
        }

        public async Task<(bool Success, string Message, SnapshotResponseDto? Data)>
            RestoreSnapshotAsync(int snapshotId, int userId, string bearerToken)
        {
            var snapshot = await _repo.GetByIdAsync(snapshotId);
            if (snapshot is null) return (false, "Snapshot not found.", null);

            var ok = await _fileClient.UpdateFileContentAsync(snapshot.FileId, snapshot.Content, bearerToken);
            if (!ok) return (false, "Could not restore file content in FileService.", null);

            var newSnapshot = new Snapshot
            {
                FileId          = snapshot.FileId,
                Content         = snapshot.Content,
                Timestamp       = DateTime.UtcNow,
                CreatedByUserId = userId,
                Message         = $"Restored from snapshot #{snapshotId}"
            };

            var created = await _repo.CreateAsync(newSnapshot);
            return (true, "Snapshot restored and new snapshot created.", MapToDto(created));
        }

        private static SnapshotResponseDto MapToDto(Snapshot s) => new()
        {
            Id              = s.Id,
            FileId          = s.FileId,
            Content         = s.Content,
            Timestamp       = s.Timestamp,
            CreatedByUserId = s.CreatedByUserId,
            Message         = s.Message
        };
    }
}
