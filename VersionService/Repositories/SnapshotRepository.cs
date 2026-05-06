using Microsoft.EntityFrameworkCore;
using VersionService.Data;
using VersionService.Models;

namespace VersionService.Repositories
{
    public interface ISnapshotRepository
    {
        Task<Snapshot>         CreateAsync(Snapshot snapshot);
        Task<Snapshot?>        GetByIdAsync(int id);
        Task<List<Snapshot>>   GetByFileIdAsync(int fileId);
    }

    public class SnapshotRepository : ISnapshotRepository
    {
        private readonly VersionDbContext _db;

        public SnapshotRepository(VersionDbContext db) => _db = db;

        public async Task<Snapshot> CreateAsync(Snapshot snapshot)
        {
            _db.Snapshots.Add(snapshot);
            await _db.SaveChangesAsync();
            return snapshot;
        }

        public async Task<Snapshot?> GetByIdAsync(int id)
            => await _db.Snapshots.FindAsync(id);

        public async Task<List<Snapshot>> GetByFileIdAsync(int fileId)
            => await _db.Snapshots
                        .Where(s => s.FileId == fileId)
                        .OrderByDescending(s => s.Timestamp)
                        .ToListAsync();
    }
}
