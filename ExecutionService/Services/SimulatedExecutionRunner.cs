using ExecutionService.Models;

namespace ExecutionService.Services
{
    public class SimulatedExecutionRunner : IExecutionRunner
    {
        public async Task<(string? Output, string? ErrorOutput, bool Success)> ExecuteAsync(
            ExecutionJob job,
            CancellationToken cancellationToken)
        {
            await Task.Delay(500, cancellationToken);

            if (job.Code.Contains("throw", StringComparison.OrdinalIgnoreCase) ||
                job.Code.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                job.Code.Contains("fail", StringComparison.OrdinalIgnoreCase))
            {
                return (null, $"Simulated {job.Language} execution failed.", false);
            }

            var output = job.Language.Trim().ToLowerInvariant() switch
            {
                "csharp" or "cs" => "Simulated C# execution completed.",
                "javascript" or "js" => "Simulated JavaScript execution completed.",
                "python" or "py" => "Simulated Python execution completed.",
                _ => $"Simulated {job.Language} execution completed."
            };

            return ($"{output}{Environment.NewLine}Code length: {job.Code.Length} characters.", null, true);
        }
    }
}
