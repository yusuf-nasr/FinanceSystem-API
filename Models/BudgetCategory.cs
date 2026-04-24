namespace FinanceSystem_Dotnet.Models
{
    public class BudgetCategory
    {
        public string Name { get; set; }

        public virtual ICollection<BudgetEntry> Entries { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
