using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Seen { get; set; } = false;
        public NotificationType Type { get; set; } = NotificationType.INFO;
        public string Code { get; set; }
        public string? Args { get; set; } // JSON serialized

        public virtual User User { get; set; }
    }
}
