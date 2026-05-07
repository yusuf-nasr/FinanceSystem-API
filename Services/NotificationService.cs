using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceSystem_Dotnet.Services
{
    public class NotificationService : INotificationService
    {
        private readonly FinanceDbContext _context;

        public NotificationService(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<NotificationDto> Create(int userId, NotificationType type, string code, string? args = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Code = code,
                Args = args,
                Timestamp = DateTime.UtcNow,
                Seen = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return new NotificationDto(notification);
        }

        public async Task<PaginatedResult<NotificationDto>> FindAll(int userId, NotificationQueryDto queryDto)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (queryDto.StartDate.HasValue)
                query = query.Where(n => n.Timestamp >= queryDto.StartDate.Value);

            if (queryDto.EndDate.HasValue)
                query = query.Where(n => n.Timestamp <= queryDto.EndDate.Value);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.Timestamp)
                .Skip((queryDto.Page - 1) * queryDto.PerPage)
                .Take(queryDto.PerPage)
                .Select(n => new NotificationDto(n))
                .ToListAsync();

            return new PaginatedResult<NotificationDto>
            {
                Data = items,
                Pagination = new PaginationMeta
                {
                    Total = totalCount,
                    LastPage = (int)Math.Ceiling((double)totalCount / queryDto.PerPage),
                    CurrentPage = queryDto.Page,
                    PerPage = queryDto.PerPage,
                    Prev = queryDto.Page > 1 ? queryDto.Page - 1 : null,
                    Next = queryDto.Page < (int)Math.Ceiling((double)totalCount / queryDto.PerPage) ? queryDto.Page + 1 : null
                }
            };
        }

        public async Task<NotificationDto> UpdateSeen(int id, int userId, bool seen)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                throw new ApiException(404, ErrorCode.NOTIFICATION_NOT_FOUND);

            notification.Seen = seen;
            await _context.SaveChangesAsync();

            return new NotificationDto(notification);
        }
    }
}
