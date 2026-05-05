using ExecutionService.DTOs;

namespace ExecutionService.Services
{
    public interface IExecutionService
    {
        Task<(bool Success, string Message, ExecutionJobResponseDto? Data)> SubmitAsync(int userId, SubmitExecutionJobDto dto);
        Task<(bool Success, string Message, ExecutionJobResponseDto? Data)> GetResultAsync(int jobId, int userId);
        Task<(bool Success, string Message, List<ExecutionJobResponseDto> Data)> GetHistoryAsync(int userId);
    }
}
