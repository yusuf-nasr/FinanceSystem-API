namespace FinanceSystem_Dotnet.Models
{
    public class BudgetEntry
    {
        public int Id { get; set; }
        public int InputterId { get; set; }
        public double Amount { get; set; }
        public string BudgetName { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual User Inputter { get; set; }
        public virtual BudgetCategory Budget { get; set; }
    }
}
