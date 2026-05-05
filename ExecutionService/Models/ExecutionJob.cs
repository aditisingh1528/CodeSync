namespace ExecutionService.Models
{
    public static class ExecutionJobStatus
    {
        public const string Queued = "QUEUED";
        public const string Running = "RUNNING";
        public const string Completed = "COMPLETED";
        public const string Failed = "FAILED";
    }

    public class ExecutionJob
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Status { get; set; } = ExecutionJobStatus.Queued;
        public string? Output { get; set; }
        public string? ErrorOutput { get; set; }
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
    }
}
