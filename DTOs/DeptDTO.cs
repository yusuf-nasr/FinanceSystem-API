namespace FinanceSystem_Dotnet.DTOs
{
    public class DeptResponseDTO
    {
        public string Name { get; set; }
        public int? ManagerId { get; set; }
    }
    public class DeptUpdateDTO
    {
        public string Name { get; set; }
        public int? ManagerId { get; set; }
    }
    public class DeptCreateDTO
    {
        public string Name { get; set; }
        public int? ManagerId { get; set; }
    }
}
