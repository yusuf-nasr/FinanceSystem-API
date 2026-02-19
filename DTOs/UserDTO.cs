using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Models;

namespace FinanceSystem_Dotnet.DTOs
{
    public class UserCreateDTO
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public Role role { get; set; }
        public string DepartmentName { get; set; }
    }
    public class UserUpdateDTO: UserCreateDTO
    {
        public bool Active{ get; set; }
    }
    public class UserLoginDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }

    }
    public class UserResponseDTO
    {
        public UserResponseDTO(User user)
        {
            Id = user.Id;
            Name = user.Name;
            role = user.Role;
            CreatedAt = user.CreatedAt.ToLocalTime();
            LastLogin = user.LastLogin.Value.ToLocalTime();
            Active = user.Active;
            DepartmentName = user.DepartmentName;

        }
        public int Id { get; set; }
        public string Name { get; set; }
        public Role role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool Active { get; set; }
        public string DepartmentName { get; set; }
    }
}
