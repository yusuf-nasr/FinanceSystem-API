using System.Text.Json.Serialization;

namespace FinanceSystem_Dotnet.DTOs
{
    public class DocumentResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }

        [JsonPropertyName("downloadURI")]
        public string DownloadURI { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UploaderId { get; set; }
    }
}
