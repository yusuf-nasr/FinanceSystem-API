namespace FinanceSystem_Dotnet.DTOs
{
    public class TransactionTypeCreateDTO
    {
        public string Name { get; set; }
    }
    public class TransactionTypeResponseDTO
    {
        public string Name { get; set; }
        public int CreatorId { get; set; }
    }
}
