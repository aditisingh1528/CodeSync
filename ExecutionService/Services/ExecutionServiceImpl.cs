using ExecutionService.DTOs;
using ExecutionService.Messaging;
using ExecutionService.Models;
using ExecutionService.Repositories;

namespace ExecutionService.Services
{
    public class ExecutionServiceImpl : IExecutionService
    {
        private readonly IExecutionJobRepository _repo;
        private readonly IExecutionJobPublisher _publisher;

        public ExecutionServiceImpl(IExecutionJobRepository repo, IExecutionJobPublisher publisher)
        {
            _repo = repo;
            _publisher = publisher;
        }

        public async Task<(bool Success, string Message, ExecutionJobResponseDto? Data)>
            SubmitAsync(int userId, SubmitExecutionJobDto dto)
        {
            var job = new ExecutionJob
            {
                Code = dto.Code,
                Language = dto.Language.Trim(),
                Status = ExecutionJobStatus.Queued,
                UserId = userId,
                ProjectId = dto.ProjectId,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.CreateAsync(job);

            try
            {
                await _publisher.PublishAsync(created.Id);
            }
            catch (Exception ex)
            {
                created.Status = ExecutionJobStatus.Failed;
                created.ErrorOutput = $"Failed to queue execution job: {ex.Message}";
                created.ExecutedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(created);
                return (false, "Execution job could not be queued.", MapToDto(created));
            }

            return (true, "Execution job submitted.", MapToDto(created));
        }

        public async Task<(bool Success, string Message, ExecutionJobResponseDto? Data)>
            GetResultAsync(int jobId, int userId)
        {
            var job = await _repo.GetByIdAsync(jobId);
            if (job is null) return (false, "Execution job not found.", null);

            if (job.UserId != userId)
                return (false, "You do not have access to this execution job.", null);

            return (true, "Execution job retrieved.", MapToDto(job));
        }

        public async Task<(bool Success, string Message, List<ExecutionJobResponseDto> Data)>
            GetHistoryAsync(int userId)
        {
            var jobs = await _repo.GetHistoryByUserAsync(userId);
            return (true, "Execution history retrieved.", jobs.Select(MapToDto).ToList());
        }

        private static ExecutionJobResponseDto MapToDto(ExecutionJob job) => new()
        {
            Id = job.Id,
            Code = job.Code,
            Language = job.Language,
            Status = job.Status,
            Output = job.Output,
            ErrorOutput = job.ErrorOutput,
            UserId = job.UserId,
            ProjectId = job.ProjectId,
            CreatedAt = job.CreatedAt,
            ExecutedAt = job.ExecutedAt
        };
    }
}
