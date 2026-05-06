using System.ComponentModel.DataAnnotations;

namespace VersionService.DTOs
{
    public class CreateSnapshotDto
    {
        [Required]
        public int FileId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Message { get; set; } = string.Empty;
    }

    public class SnapshotResponseDto
    {
        public int      Id              { get; set; }
        public int      FileId          { get; set; }
        public string   Content         { get; set; } = string.Empty;
        public DateTime Timestamp       { get; set; }
        public int      CreatedByUserId { get; set; }
        public string   Message         { get; set; } = string.Empty;
    }

    public class RestoreSnapshotDto
    {
        [Required]
        public int SnapshotId { get; set; }
    }
}
