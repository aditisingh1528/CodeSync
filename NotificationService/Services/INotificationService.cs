using NotificationService.DTOs;

namespace NotificationService.Services
{
    public interface INotificationService
    {
        Task<(bool Success, string Message, List<NotificationResponseDto> Data)> GetMyNotificationsAsync(int userId);
        Task<(bool Success, string Message)>                                     MarkAsReadAsync(int notificationId, int userId);
    }
}
