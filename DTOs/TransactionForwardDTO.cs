using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Models;

namespace FinanceSystem_Dotnet.DTOs
{
    public class TransactionForwardDTO
    {
        public int Id { get; set; }
        public TransactionForwardStatus Status { get; set; }
        public string? SenderComment { get; set; }
        public string? ReceiverComment { get; set; }
        public bool SenderSeen { get; set; } = true;
        public bool ReceiverSeen { get; set; } = false;
        public DateTime ForwardedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
        public int TransactionId { get; set; }

    }
}
