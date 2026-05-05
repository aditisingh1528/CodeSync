namespace NotificationService.Models
{
    public static class NotificationType
    {
        public const string JobCompleted = "JobCompleted";
        public const string JobFailed    = "JobFailed";
    }

    public class Notification
    {
        public int      NotificationId { get; set; }
        public int      UserId         { get; set; }
        public string   Message        { get; set; } = string.Empty;
        public string   Type           { get; set; } = string.Empty;
        public bool     IsRead         { get; set; } = false;
        public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
    }
}
