using System;
using System.Collections.Generic;

namespace FinanceSystem_Dotnet.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public byte[] Content { get; set; }
        public DateTime UploadedAt { get; set; }

        public int UploaderId { get; set; }
        public virtual User Uploader { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
