using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FinanceSystem_Dotnet.Services
{
    public interface INotificationService
    {
        Task<PaginatedResult<NotificationDTO>> FindAllAsync(int userId, NotificationQueryDTO query);
        Task<NotificationDTO> UpdateSeenAsync(int id, int userId, bool seen);
        Task CreateNotificationAsync(int userId, NotificationType type, string code, object? args = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly FinanceDbContext _context;
        private readonly ISseService _sseService;

        public NotificationService(FinanceDbContext context, ISseService sseService)
        {
            _context = context;
            _sseService = sseService;
        }

        public async Task<PaginatedResult<NotificationDTO>> FindAllAsync(int userId, NotificationQueryDTO query)
        {
            var q = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.Timestamp)
                .AsQueryable();

            if (query.StartDate.HasValue)
                q = q.Where(n => n.Timestamp >= query.StartDate.Value);
            if (query.EndDate.HasValue)
                q = q.Where(n => n.Timestamp <= query.EndDate.Value);

            var total = await q.CountAsync();
            var lastPage = (int)Math.Ceiling((double)total / query.PerPage);
            var items = await q
                .Skip((query.Page - 1) * query.PerPage)
                .Take(query.PerPage)
                .ToListAsync();

            return new PaginatedResult<NotificationDTO>
            {
                Data = items.Select(MapToDTO).ToList(),
                Pagination = new PaginationMeta
                {
                    Total = total,
                    LastPage = lastPage,
                    CurrentPage = query.Page,
                    PerPage = query.PerPage,
                    Prev = query.Page > 1 ? query.Page - 1 : null,
                    Next = query.Page < lastPage ? query.Page + 1 : null
                }
            };
        }

        public async Task<NotificationDTO> UpdateSeenAsync(int id, int userId, bool seen)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                throw new Exceptions.ApiException(404, ErrorCode.NOTIFICATION_NOT_FOUND,
                    new Dictionary<string, object> { { "notificationId", id.ToString() } });

            notification.Seen = seen;
            await _context.SaveChangesAsync();

            return MapToDTO(notification);
        }

        public async Task CreateNotificationAsync(int userId, NotificationType type, string code, object? args = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Code = code,
                Args = args != null ? JsonSerializer.Serialize(args) : null,
                Timestamp = DateTime.UtcNow,
                Seen = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Emit via SSE to the user
            _sseService.EmitToUser(userId, "notification", MapToDTO(notification));
        }

        private NotificationDTO MapToDTO(Notification n)
        {
            return new NotificationDTO
            {
                Id = n.Id,
                UserId = n.UserId,
                Timestamp = n.Timestamp,
                Seen = n.Seen,
                Type = n.Type,
                Code = n.Code,
                Args = n.Args != null ? JsonSerializer.Deserialize<object>(n.Args) : null
            };
        }
    }
}
