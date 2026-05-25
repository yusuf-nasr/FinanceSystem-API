namespace FinanceSystem_Dotnet.DTOs
{
    public class DeptResponseDTO
    {
        public string Name { get; set; }
        public int? ManagerId { get; set; }
    }
    public class DeptUpdateDTO
    {
        public string? Name { get; set; }
        public int? ManagerId { get; set; }
    }
    public class DeptCreateDTO
    {
        public string Name { get; set; }
        public int? ManagerId { get; set; }
    }
    public class DeptQueryDTO
    {
        public string? Name { get; set; }
        public string? Manager { get; set; }
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 10;
    }
}
