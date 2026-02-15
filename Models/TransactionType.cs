using System.Collections.Generic;

namespace FinanceSystem_Dotnet.Models
{
    public class TransactionType
    {
        public string Name { get; set; }
        public int CreatorId { get; set; }
        
        public virtual User Creator { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
