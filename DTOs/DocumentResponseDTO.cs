namespace FinanceSystem_Dotnet.DTOs
{
    public class DocumentResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string URI { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UploaderId { get; set; }

    }
}
