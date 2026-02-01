using System;

namespace FinanceSystem_Dotnet.Models
{
    public class Document
    {
        public int Id { get; set; }
        public byte[] Content { get; set; }
        public DateTime UploadedAt { get; set; }

        public string UploaderName { get; set; }
        public virtual User Uploader { get; set; }

        public int TransactionId { get; set; }
        public virtual Transaction Transaction { get; set; }
    }
}
