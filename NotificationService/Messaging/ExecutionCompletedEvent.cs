namespace NotificationService.Messaging
{
    // This matches exactly what ExecutionService publishes after a job finishes.
    public class ExecutionCompletedEvent
    {
        public int    JobId   { get; set; }
        public int    UserId  { get; set; }
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
