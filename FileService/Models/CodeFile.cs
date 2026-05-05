namespace FileService.Models
{
    /// <summary>
    /// Represents both files AND folders in the project file tree.
    /// IsFolder = true  → it's a folder (no content)
    /// IsFolder = false → it's a file  (has content)
    /// ParentFolderId is null when it's a root-level item.
    /// </summary>
    public class CodeFile
    {
        public int      Id             { get; set; }
        public string   Name           { get; set; } = string.Empty;

        // Only files have content. Folders store null here.
        public string?  Content        { get; set; }

        // Which project does this belong to?
        public int      ProjectId      { get; set; }

        // null = root level. Set to a folder's Id to nest inside it.
        public int?     ParentFolderId { get; set; }

        // true = folder, false = file — drives all tree logic
        public bool     IsFolder       { get; set; }

        public DateTime CreatedAt      { get; set; }
        public DateTime UpdatedAt      { get; set; }

        // Who created this? Stored for future collaboration features.
        public int      CreatedByUserId { get; set; }

        // Navigation: children of this folder (EF Core fills this)
        public List<CodeFile> Children { get; set; } = new();
    }
}
