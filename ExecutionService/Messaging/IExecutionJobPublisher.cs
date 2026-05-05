namespace ExecutionService.Messaging
{
    public interface IExecutionJobPublisher
    {
        Task PublishAsync(int jobId, int attempt = 1, CancellationToken cancellationToken = default);
    }
}
