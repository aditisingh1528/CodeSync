using FileService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileService.Data
{
    public class FileDbContext : DbContext
    {
        public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }

        public DbSet<CodeFile> CodeFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CodeFile>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)    .IsRequired().HasMaxLength(200);
                entity.Property(e => e.IsFolder) .IsRequired();
                entity.Property(e => e.Content)  .HasColumnType("nvarchar(max)");

                // Fast lookup: all files in a project
                entity.HasIndex(e => e.ProjectId);

                // Fast lookup: children of a folder
                entity.HasIndex(e => e.ParentFolderId);

                // Self-referencing FK: a file/folder can live inside a folder
                // Restrict prevents accidentally cascade-deleting whole tree
                entity.HasOne<CodeFile>()
                      .WithMany(e => e.Children)
                      .HasForeignKey(e => e.ParentFolderId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);
            });
        }
    }
}
