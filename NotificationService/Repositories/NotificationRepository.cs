using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;

namespace NotificationService.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationDbContext _db;

        public NotificationRepository(NotificationDbContext db) => _db = db;

        public async Task<List<Notification>> GetByUserIdAsync(int userId)
            => await _db.Notifications
                        .Where(n => n.UserId == userId)
                        .OrderByDescending(n => n.CreatedAt)
                        .ToListAsync();

        public async Task<Notification?> GetByIdAsync(int notificationId)
            => await _db.Notifications.FindAsync(notificationId);

        public async Task<Notification> CreateAsync(Notification notification)
        {
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
            return notification;
        }

        public async Task<Notification> UpdateAsync(Notification notification)
        {
            _db.Notifications.Update(notification);
            await _db.SaveChangesAsync();
            return notification;
        }
    }
}
