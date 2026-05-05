using ExecutionService.Models;

namespace ExecutionService.Services
{
    public interface IExecutionRunner
    {
        Task<(string? Output, string? ErrorOutput, bool Success)> ExecuteAsync(ExecutionJob job, CancellationToken cancellationToken);
    }
}
