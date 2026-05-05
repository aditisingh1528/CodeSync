using ExecutionService.Models;

namespace ExecutionService.Repositories
{
    public interface IExecutionJobRepository
    {
        Task<ExecutionJob> CreateAsync(ExecutionJob job);
        Task<ExecutionJob?> GetByIdAsync(int id);
        Task<List<ExecutionJob>> GetHistoryByUserAsync(int userId);
        Task<ExecutionJob> UpdateAsync(ExecutionJob job);
    }
}
