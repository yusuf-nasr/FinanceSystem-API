using FinanceSystem_Dotnet.Enums;
using System;
using System.Collections.Generic;

namespace FinanceSystem_Dotnet.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Fulfilled { get; set; }
        public TransactionPriority Priority { get; set; }
        public DateTime CreatedAt { get; set; }

        public string CreatorName { get; set; }
        public virtual User Creator { get; set; }

        public string TransactionTypeName { get; set; }
        public virtual TransactionType TransactionType { get; set; }

        public virtual ICollection<Document> Documents { get; set; }
        public virtual ICollection<TransactionForward> Forwards { get; set; }
    }
}
