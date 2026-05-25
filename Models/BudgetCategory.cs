namespace FinanceSystem_Dotnet.Models
{
    public class BudgetCategory
    {
        public string Name { get; set; }
        public double Preallocation { get; set; } = 0;

        public virtual ICollection<BudgetEntry> Entries { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
