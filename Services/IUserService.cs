using FinanceSystem_Dotnet.DTOs;

namespace FinanceSystem_Dotnet.Services
{
    public interface IUserService
    {
        Task<(bool Success, string Message)> CreateUserAsync(UserCreateDTO request);
        Task<List<UserResponseDTO>> GetAllUsersAsync();
        Task<UserResponseDTO?> GetUserByIdAsync(int id);
        Task<(bool Success, string Message)> UpdateUserAsync(int id, UserUpdateDTO request, bool isAdmin);
        Task<(bool Success, string Message)> DeleteUserAsync(int id);
    }
}
