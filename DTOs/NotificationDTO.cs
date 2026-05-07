using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Models;
using System;

namespace FinanceSystem_Dotnet.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Seen { get; set; }
        public NotificationType Type { get; set; }
        public string Code { get; set; }
        public string? Args { get; set; }

        public NotificationDto() { }
        public NotificationDto(Notification notification)
        {
            Id = notification.Id;
            UserId = notification.UserId;
            Timestamp = notification.Timestamp;
            Seen = notification.Seen;
            Type = notification.Type;
            Code = notification.Code;
            Args = notification.Args;
        }
    }

    public class UpdateSeenDto
    {
        public bool Seen { get; set; }
    }

    public class NotificationQueryDto
    {
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 10;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
