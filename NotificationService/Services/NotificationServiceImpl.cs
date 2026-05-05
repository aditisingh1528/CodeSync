using NotificationService.DTOs;
using NotificationService.Repositories;

namespace NotificationService.Services
{
    public class NotificationServiceImpl : INotificationService
    {
        private readonly INotificationRepository _repo;

        public NotificationServiceImpl(INotificationRepository repo) => _repo = repo;

        public async Task<(bool Success, string Message, List<NotificationResponseDto> Data)>
            GetMyNotificationsAsync(int userId)
        {
            var notifications = await _repo.GetByUserIdAsync(userId);
            var dtos = notifications.Select(n => new NotificationResponseDto
            {
                NotificationId = n.NotificationId,
                UserId         = n.UserId,
                Message        = n.Message,
                Type           = n.Type,
                IsRead         = n.IsRead,
                CreatedAt      = n.CreatedAt
            }).ToList();

            return (true, "Notifications retrieved.", dtos);
        }

        public async Task<(bool Success, string Message)>
            MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _repo.GetByIdAsync(notificationId);

            if (notification is null)
                return (false, "Notification not found.");

            if (notification.UserId != userId)
                return (false, "You do not have access to this notification.");

            notification.IsRead = true;
            await _repo.UpdateAsync(notification);

            return (true, "Notification marked as read.");
        }
    }
}
