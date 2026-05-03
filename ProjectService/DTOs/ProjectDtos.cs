using System.ComponentModel.DataAnnotations;

namespace ProjectService.DTOs
{
    // ─────────────────────────────────────────────────────────────────────
    // CREATE PROJECT REQUEST
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Sent by client when creating a new project.
    /// UserId is NOT here — it comes from the JWT token automatically.
    /// </summary>
    public class CreateProjectDto
    {
        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────
    // UPDATE PROJECT REQUEST
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Sent by client when updating an existing project.
    /// Only Name and Description can be changed — UserId never changes.
    /// </summary>
    public class UpdateProjectDto
    {
        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────
    // PROJECT RESPONSE
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Returned to the client in all responses.
    /// We map from Project entity → ProjectResponseDto so we control
    /// exactly what fields the client sees.
    /// </summary>
    public class ProjectResponseDto
    {
        public int    Id          { get; set; }
        public string Name        { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int    UserId      { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
