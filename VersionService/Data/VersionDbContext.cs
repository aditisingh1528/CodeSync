using Microsoft.EntityFrameworkCore;
using VersionService.Models;

namespace VersionService.Data
{
    public class VersionDbContext : DbContext
    {
        public VersionDbContext(DbContextOptions<VersionDbContext> options) : base(options) { }

        public DbSet<Snapshot> Snapshots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Snapshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.FileId);
                entity.HasIndex(e => e.CreatedByUserId);
                entity.HasIndex(e => e.Timestamp);
            });
        }
    }
}
