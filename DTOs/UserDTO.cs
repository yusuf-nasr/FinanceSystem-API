using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Models;

namespace FinanceSystem_Dotnet.DTOs
{
    public class UserCreateDTO
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public Role Role { get; set; }
        public string DepartmentName { get; set; }
    }
    public class UserUpdateDTO
    {
        public string? Name { get; set; }
        public string? Password { get; set; }
        public Role? Role { get; set; }
        public bool? Active { get; set; }
        public string? DepartmentName { get; set; }
    }
    public class UserLoginDTO
    {
        public string Name { get; set; }
        public string Password { get; set; }

    }
    public class UserQueryDTO
    {
        public string? Name { get; set; }
        public string? Department { get; set; }
        public Role? Role { get; set; }
        public bool? Active { get; set; }
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 10;
    }
    public class UserResponseDTO
    {
        public UserResponseDTO() { }

        public UserResponseDTO(User user)
        {
            Id = user.Id;
            Name = user.Name;
            Role = user.Role;
            Presence = user.Presence;
            CreatedAt = user.CreatedAt;
            LastLogin = user.LastLogin;
            Active = user.Active;
            DepartmentName = user.DepartmentName;

        }
        public int Id { get; set; }
        public string Name { get; set; }
        public Role Role { get; set; }
        public UserPresence Presence { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool Active { get; set; }
        public string DepartmentName { get; set; }
    }
}
