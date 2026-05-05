using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>(e =>
            {
                e.HasKey(n => n.NotificationId);
                e.Property(n => n.Message).IsRequired().HasMaxLength(500);
                e.Property(n => n.Type).IsRequired().HasMaxLength(50);
                e.HasIndex(n => n.UserId);
            });
        }
    }
}
