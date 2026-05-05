using System.ComponentModel.DataAnnotations;

namespace ExecutionService.DTOs
{
    public class SubmitExecutionJobDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Language { get; set; } = string.Empty;

        [Required]
        public int ProjectId { get; set; }
    }

    public class ExecutionJobResponseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Output { get; set; }
        public string? ErrorOutput { get; set; }
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
    }
}
