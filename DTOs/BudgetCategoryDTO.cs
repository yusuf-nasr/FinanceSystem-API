namespace FinanceSystem_Dotnet.DTOs
{
    // Convenience aliases for paginated results
    public class BudgetCategoryListResultDTO : PaginatedResult<BudgetCategoryDTO> { }
    public class BudgetEntryListResultDTO : PaginatedResult<BudgetEntryDTO> { }

    public class BudgetCategoryDTO
    {
        public string Name { get; set; }
        public double Budget { get; set; }
        public double Allocated { get; set; }
        public double Available { get; set; }
    }

    public class BudgetEntryDTO
    {
        public int Id { get; set; }
        public int InputterId { get; set; }
        public double Amount { get; set; }
        public string BudgetName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateBudgetEntryDTO
    {
        public double Amount { get; set; }
    }

    public class UpdateBudgetCategoryDTO
    {
        public string NewName { get; set; }
    }

    public class BudgetCategoryQueryDTO
    {
        public string? Name { get; set; }
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 10;
    }

    public class BudgetEntryQueryDTO
    {
        public string? Inputter { get; set; }
        public double? MinAmount { get; set; }
        public double? MaxAmount { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 10;
    }
}
