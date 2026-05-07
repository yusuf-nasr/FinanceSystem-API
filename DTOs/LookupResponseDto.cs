namespace FinanceSystem_Dotnet.DTOs
{
    public class LookupResponseDto
    {
        public string[] UserRole { get; set; }
        public string[] TransactionPriority { get; set; }
        public string[] TransactionForwardStatus { get; set; }
        public string[] UserPresence { get; set; }
        public string[] NotificationType { get; set; }
    }
}
