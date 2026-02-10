using FinanceSystem_Dotnet.Enums;
using System;

namespace FinanceSystem_Dotnet.Models
{
    public class TransactionForward
    {
        public int Id { get; set; }
        public TransactionForwardStatus Status { get; set; }
        public string? SenderComment { get; set; }
        public string? ReceiverComment { get; set; }
        public bool Seen { get; set; }
        public DateTime ForwardedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string SenderName { get; set; }
        public virtual User Sender { get; set; }

        public string ReceiverName { get; set; }
        public virtual User Receiver { get; set; }

        public int TransactionId { get; set; }
        public virtual Transaction Transaction { get; set; }
    }
}
