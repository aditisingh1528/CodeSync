namespace ExecutionService.Messaging
{
    public class ProjectDeletedEvent
    {
        public int      ProjectId { get; set; }
        public int      UserId    { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
