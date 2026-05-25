using FinanceSystem_Dotnet.Enums;
using System.Text.Json.Serialization;

namespace FinanceSystem_Dotnet.DTOs
{
    public class TransactionDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Fulfilled { get; set; }
        public TransactionPriority Priority { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TransactionForwardStatus? LastForwardStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatorId { get; set; }
        [JsonPropertyName("typeName")]
        public string TransactionTypeName { get; set; }
        public string? BudgetName { get; set; }
        public double? BudgetAllocation { get; set; }
        public virtual ICollection<DocumentResponseDTO> Documents { get; set; }
    }

    public class TransactionListItemDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool Fulfilled { get; set; }
        public TransactionPriority Priority { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TransactionForwardStatus? LastForwardStatus { get; set; }
        [JsonPropertyName("typeName")]
        public string TransactionTypeName { get; set; }
        public int DocumentsCount { get; set; }
        public DateTime CreatedAt { get; set; }
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
        [JsonPropertyName("typeName")]
        public string TransactionTypeName { get; set; }
        public TransactionPriority Priority { get; set; }
        [JsonPropertyName("documentsIds")]
        public IEnumerable<int>? DocumentIds { get; set; }
    }

    public class TransactionUpdateDTO
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        [JsonPropertyName("typeName")]
        public string? TransactionTypeName { get; set; }
        public TransactionPriority? Priority { get; set; }
        public bool? Fulfilled { get; set; }
        public string? BudgetName { get; set; }
        public double? BudgetAllocation { get; set; }
    }

    public class TransactionFilterDTO
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? TypeName { get; set; }
        public bool? Fulfilled { get; set; }
        public TransactionPriority? Priority { get; set; }
        public int? CreatorId { get; set; }
        public TransactionForwardStatus? LastForwardStatus { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public TransactionQuery? Query { get; set; }
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 10;
    }
}
