using FinanceSystem_Dotnet.Enums;
using System;

namespace FinanceSystem_Dotnet.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public virtual User User { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Seen { get; set; }
        public NotificationType Type { get; set; }
        public string Code { get; set; }
        public string? Args { get; set; }
    }
}
