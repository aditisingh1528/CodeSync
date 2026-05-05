using ExecutionService.Data;
using ExecutionService.Models;
using Microsoft.EntityFrameworkCore;

namespace ExecutionService.Repositories
{
    public class ExecutionJobRepository : IExecutionJobRepository
    {
        private readonly ExecutionDbContext _db;

        public ExecutionJobRepository(ExecutionDbContext db) => _db = db;

        public async Task<ExecutionJob> CreateAsync(ExecutionJob job)
        {
            _db.ExecutionJobs.Add(job);
            await _db.SaveChangesAsync();
            return job;
        }

        public async Task<ExecutionJob?> GetByIdAsync(int id)
            => await _db.ExecutionJobs.FindAsync(id);

        public async Task<List<ExecutionJob>> GetHistoryByUserAsync(int userId)
            => await _db.ExecutionJobs
                        .Where(j => j.UserId == userId)
                        .OrderByDescending(j => j.CreatedAt)
                        .ToListAsync();

        public async Task<ExecutionJob> UpdateAsync(ExecutionJob job)
        {
            _db.ExecutionJobs.Update(job);
            await _db.SaveChangesAsync();
            return job;
        }
    }
}
