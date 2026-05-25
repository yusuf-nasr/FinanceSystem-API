using System.Text.Json.Serialization;

namespace FinanceSystem_Dotnet.DTOs
{
    public class LookupResponseDto
    {
        [JsonPropertyName("UserRole")]
        public string[] UserRole { get; set; }

        [JsonPropertyName("TransactionPriority")]
        public string[] TransactionPriority { get; set; }

        [JsonPropertyName("TransactionForwardStatus")]
        public string[] TransactionForwardStatus { get; set; }
    }
}
