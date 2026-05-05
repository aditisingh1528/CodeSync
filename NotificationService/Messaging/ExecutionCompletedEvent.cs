namespace NotificationService.Messaging
{
    public class ExecutionCompletedEvent
    {
        public int    JobId   { get; set; }
        public int    UserId  { get; set; }
        public bool   Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
