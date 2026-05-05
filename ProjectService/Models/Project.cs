namespace ProjectService.Models
{    public class Project
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Updated every time Name or Description changes
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
