namespace ProjectService.Models
{
    /// <summary>
    /// PROJECT ENTITY
    /// ==============
    /// This is the main table in ProjectDB.
    ///
    /// Fields:
    ///   Id          — primary key, auto-incremented by SQL Server
    ///   Name        — project name, required, max 100 chars
    ///   Description — optional description of the project
    ///   UserId      — the ID of the user who OWNS this project
    ///                 (taken from JWT claim: ClaimTypes.NameIdentifier)
    ///                 ProjectService does NOT store users — it trusts
    ///                 the JWT token issued by AuthService
    ///   CreatedAt   — set once on creation, never changed
    ///   UpdatedAt   — updated every time the project is modified
    /// </summary>
    public class Project
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // UserId comes from the JWT token — it is the authenticated user's Id
        // from AuthService. We do NOT have a foreign key to a Users table
        // because Users live in AuthDB, not ProjectDB.
        public int UserId { get; set; }

        // Set automatically in the repository on Create — never changed after
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Updated every time Name or Description changes
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
