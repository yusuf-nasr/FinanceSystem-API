using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.DTOs
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Seen { get; set; }
        public NotificationType Type { get; set; }
        public string Code { get; set; }
        public object? Args { get; set; }
    }

    public class NotificationQueryDTO
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 10;
    }

    public class UpdateNotificationSeenDTO
    {
        public bool Seen { get; set; }
    }
}
