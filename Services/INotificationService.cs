using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.Services
{
    public interface INotificationService
    {
        Task<NotificationDto> Create(int userId, NotificationType type, string code, string? args = null);
        Task<PaginatedResult<NotificationDto>> FindAll(int userId, NotificationQueryDto queryDto);
        Task<NotificationDto> UpdateSeen(int id, int userId, bool seen);
    }
}
