using ExecutionService.Models;
using Microsoft.EntityFrameworkCore;

namespace ExecutionService.Data
{
    public class ExecutionDbContext : DbContext
    {
        public ExecutionDbContext(DbContextOptions<ExecutionDbContext> options) : base(options) { }

        public DbSet<ExecutionJob> ExecutionJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExecutionJob>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Code).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(e => e.Language).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Output).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ErrorOutput).HasColumnType("nvarchar(max)");

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}
