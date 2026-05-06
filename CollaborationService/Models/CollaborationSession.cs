namespace CollaborationService.Models
{
    public class CollaborationSession
    {
        public string SessionId  { get; set; } = string.Empty;
        public int    FileId     { get; set; }
        public int    ProjectId  { get; set; }
        public int    OwnerId    { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<int> JoinedUserIds { get; set; } = new();
    }
}
