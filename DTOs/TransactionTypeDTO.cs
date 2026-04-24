namespace FinanceSystem_Dotnet.DTOs
{
    public class TransactionTypeCreateDTO
    {
        public string Name { get; set; }
    }
    public class TransactionTypeQueryDTO
    {
        public int? CreatorId { get; set; }
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 10;
    }
    public class TransactionTypeResponseDTO
    {
        public string Name { get; set; }
        public int CreatorId { get; set; }
    }
}
