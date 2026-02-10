using FinanceSystem_Dotnet.Enums;
using System.Collections.Generic;

namespace FinanceSystem_Dotnet.Models
{
    public class User
    {
        public string Name { get; set; }
        public string HashedPassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Active { get; set; }
        public Role Role { get; set; }
        public DateTime? LastLogin { get; set; }

        public string DepartmentName { get; set; }
        public virtual Department Department { get; set; }

        public virtual Department ManagedDepartment { get; set; }
        public virtual ICollection<Transaction> CreatedTransactions { get; set; }
        public virtual ICollection<TransactionType> CreatedTransactionTypes { get; set; }
        public virtual ICollection<Document> UploadedDocuments { get; set; }
        public virtual ICollection<TransactionForward> SentForwards { get; set; }
        public virtual ICollection<TransactionForward> ReceivedForwards { get; set; }
    }
}
