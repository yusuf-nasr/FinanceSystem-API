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
        public UserResponseDTO Sender { get; set; }
        public UserResponseDTO Receiver { get; set; }
        public int TransactionId { get; set; }

    }

    public class TransactionForwardCreateDTO
    {
        public int ReceiverId { get; set; }
        public string? Comment { get; set; }
    }

    public class TransactionForwardUpdateDTO
    {
        public TransactionForwardStatus Status { get; set; }
        public string? Comment { get; set; }
    }

    public class TransactionForwardSenderUpdateDTO
    {
        public string? Comment { get; set; }
    }
}
