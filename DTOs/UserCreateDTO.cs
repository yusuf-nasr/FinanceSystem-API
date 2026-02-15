using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.DTOs
{
    public class UserCreateDTO
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public Role role { get; set; }
        public string DepartmentName { get; set; }
    }
}
