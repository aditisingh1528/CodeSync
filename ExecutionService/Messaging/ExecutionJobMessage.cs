namespace ExecutionService.Messaging
{
    public class ExecutionJobMessage
    {
        public int JobId { get; set; }
        public int Attempt { get; set; } = 1;
    }
}
