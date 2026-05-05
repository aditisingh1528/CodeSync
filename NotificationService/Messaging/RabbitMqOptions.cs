namespace NotificationService.Messaging
{
    public class RabbitMqOptions
    {
        public string HostName              { get; set; } = "localhost";
        public int    Port                  { get; set; } = 5672;
        public string UserName              { get; set; } = "guest";
        public string Password              { get; set; } = "guest";
        public string ExecutionEventsQueue  { get; set; } = "execution.events";
        public int    RetryDelayMilliseconds { get; set; } = 5000;
    }
}
