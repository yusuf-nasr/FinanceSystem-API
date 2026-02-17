using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Models;

namespace FinanceSystem_Dotnet.DTOs
{
    public class TransactionDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Fulfilled { get; set; }
        public TransactionPriority Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatorId { get; set; }
        public string TransactionTypeName { get; set; }
        public virtual ICollection<Document> Documents { get; set; }

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
