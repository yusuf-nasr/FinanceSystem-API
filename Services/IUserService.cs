using FinanceSystem_Dotnet.DTOs;

namespace FinanceSystem_Dotnet.Services
{
    public interface IUserService
    {
        Task<UserResponseDTO> CreateUserAsync(UserCreateDTO request);
        Task<PaginatedResult<UserResponseDTO>> FindAllAsync(UserQueryDTO query);
        Task<UserResponseDTO?> GetUserByIdAsync(int id);
        Task<UserResponseDTO> UpdateUserAsync(int id, UserUpdateDTO request);
        Task<UserResponseDTO> DeleteUserAsync(int id);
    }
}
