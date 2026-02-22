using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.DTOs
{
    public class TransactionDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Fulfilled { get; set; }
        public TransactionPriority Priority { get; set; }
        public TransactionForwardStatus? LastForwardStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatorId { get; set; }
        public string TransactionTypeName { get; set; }
        public virtual ICollection<DocumentResponseDTO> Documents { get; set; }
    }

    public class TransactionListItemDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool Fulfilled { get; set; }
        public TransactionPriority Priority { get; set; }
        public TransactionForwardStatus? LastForwardStatus { get; set; }
        public string TransactionTypeName { get; set; }
        public int DocumentsCount { get; set; }
    }

    public class TransactionListResultDTO
    {
        public List<TransactionListItemDTO> Data { get; set; } = new();
        public PaginationMeta Pagination { get; set; } = new();
        public Dictionary<string, int> Summary { get; set; } = new();
    }

    public class TransactionCreateDTO
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string TransactionTypeName { get; set; }
        public TransactionPriority Priority { get; set; }
        public IEnumerable<int> DocumentIds { get; set; }
    }

    public class TransactionUpdateDTO
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string TransactionTypeName { get; set; }
        public TransactionPriority Priority { get; set; }
        public bool Fulfilled { get; set; }
        public IEnumerable<int> DocumentIds { get; set; }
    }
}
