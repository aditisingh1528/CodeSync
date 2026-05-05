using Microsoft.EntityFrameworkCore;
using ProjectService.Models;

namespace ProjectService.Data
{
    public class ProjectDbContext : DbContext
    {
        public ProjectDbContext(DbContextOptions<ProjectDbContext> options)
            : base(options) { }

                public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>(entity =>
            {
                
                entity.HasKey(p => p.Id);

                
                entity.Property(p => p.Name)
                      .IsRequired()
                      .HasMaxLength(100);

            
                entity.Property(p => p.Description)
                      .HasMaxLength(500)
                      .HasDefaultValue(string.Empty);

                
                entity.Property(p => p.UserId)
                      .IsRequired();

                
                entity.Property(p => p.CreatedAt)
                      .IsRequired();

                entity.Property(p => p.UpdatedAt)
                      .IsRequired();

               
                entity.HasIndex(p => p.UserId)
                      .HasDatabaseName("IX_Projects_UserId");
            });
        }
    }
}
