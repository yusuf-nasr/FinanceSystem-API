using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.DTOs
{
    public class TransactionCreateDTO
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string TransactionTypeName { get; set; }
        public TransactionPriority Priority { get; set; }
        public IEnumerable<int> DocumentIds { get; set; }
    }
}
