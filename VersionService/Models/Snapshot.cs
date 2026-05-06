namespace VersionService.Models
{
    public class Snapshot
    {
        public int      Id              { get; set; }
        public int      FileId          { get; set; }
        public string   Content         { get; set; } = string.Empty;
        public DateTime Timestamp       { get; set; }
        public int      CreatedByUserId { get; set; }
        public string   Message         { get; set; } = string.Empty;
    }
}
