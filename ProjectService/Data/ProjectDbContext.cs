using Microsoft.EntityFrameworkCore;
using ProjectService.Models;

namespace ProjectService.Data
{
    /// <summary>
    /// PROJECTDBCONTEXT
    /// =================
    /// EF Core database context for ProjectDB.
    /// This is a SEPARATE database from AuthDB — microservices own their data.
    ///
    /// ProjectDB only has one table: Projects.
    /// User data lives in AuthDB — we only store UserId here as a plain int.
    /// </summary>
    public class ProjectDbContext : DbContext
    {
        public ProjectDbContext(DbContextOptions<ProjectDbContext> options)
            : base(options) { }

        // Maps to the "Projects" table in ProjectDB
        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>(entity =>
            {
                // Primary key
                entity.HasKey(p => p.Id);

                // Name: required, max 100 chars
                entity.Property(p => p.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                // Description: optional, max 500 chars
                entity.Property(p => p.Description)
                      .HasMaxLength(500)
                      .HasDefaultValue(string.Empty);

                // UserId: required — every project must have an owner
                entity.Property(p => p.UserId)
                      .IsRequired();

                // CreatedAt / UpdatedAt: stored as UTC datetime
                entity.Property(p => p.CreatedAt)
                      .IsRequired();

                entity.Property(p => p.UpdatedAt)
                      .IsRequired();

                // Index on UserId — the most common query is
                // "get all projects for this user", so index speeds it up
                entity.HasIndex(p => p.UserId)
                      .HasDatabaseName("IX_Projects_UserId");
            });
        }
    }
}
