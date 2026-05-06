using System.ComponentModel.DataAnnotations;

namespace CollaborationService.DTOs
{
    public class CreateSessionDto
    {
        [Required]
        public int FileId    { get; set; }

        [Required]
        public int ProjectId { get; set; }
    }

    public class SessionResponseDto
    {
        public string    SessionId    { get; set; } = string.Empty;
        public int       FileId       { get; set; }
        public int       ProjectId    { get; set; }
        public int       OwnerId      { get; set; }
        public DateTime  CreatedAt    { get; set; }
        public List<int> Participants { get; set; } = new();
    }

    public class CodeChangeDto
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        public string Content   { get; set; } = string.Empty;
    }
}
