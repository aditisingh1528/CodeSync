using System.ComponentModel.DataAnnotations;

namespace FileService.DTOs
{
    // ── CREATE FILE ───────────────────────────────────────────────────────
    public class CreateFileDto
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string  Name          { get; set; } = string.Empty;
        public string? Content       { get; set; }

        [Required]
        public int     ProjectId     { get; set; }
        public int?    ParentFolderId { get; set; }
    }

    // ── CREATE FOLDER ─────────────────────────────────────────────────────
    public class CreateFolderDto
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name           { get; set; } = string.Empty;

        [Required]
        public int    ProjectId      { get; set; }
        public int?   ParentFolderId { get; set; }
    }

    // ── UPDATE CONTENT ────────────────────────────────────────────────────
    public class UpdateCodeFileDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }

    // ── RESPONSE (flat) ───────────────────────────────────────────────────
    public class CodeFileResponseDto
    {
        public int      Id              { get; set; }
        public string   Name            { get; set; } = string.Empty;
        public string?  Content         { get; set; }
        public int      ProjectId       { get; set; }
        public int?     ParentFolderId  { get; set; }
        public bool     IsFolder        { get; set; }
        public DateTime CreatedAt       { get; set; }
        public DateTime UpdatedAt       { get; set; }
        public int      CreatedByUserId { get; set; }
    }

    // ── TREE NODE ─────────────────────────────────────────────────────────
    // Used for GET /api/files/tree/{projectId}
    // Each node can have children (folders contain files/folders inside them)
    public class FileTreeNodeDto
    {
        public int      Id              { get; set; }
        public string   Name            { get; set; } = string.Empty;
        public bool     IsFolder        { get; set; }
        public int?     ParentFolderId  { get; set; }
        public string?  Content         { get; set; }   // null for folders
        public int      CreatedByUserId { get; set; }

        // Recursively nested children — empty list for files
        public List<FileTreeNodeDto> Children { get; set; } = new();
    }
}
