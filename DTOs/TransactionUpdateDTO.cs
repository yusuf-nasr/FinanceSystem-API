using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.DTOs
{
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
