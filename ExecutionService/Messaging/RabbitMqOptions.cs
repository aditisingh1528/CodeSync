namespace ExecutionService.Messaging
{
    public class RabbitMqOptions
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string QueueName { get; set; } = "execution.jobs";
        public int MaxRetries { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 500;
    }
}
