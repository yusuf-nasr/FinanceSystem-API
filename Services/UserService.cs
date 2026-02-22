using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceSystem_Dotnet.Services
{
    public class UserService : IUserService
    {
        private readonly FinanceDbContext _context;

        public UserService(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(UserCreateDTO request)
        {
            if (_context.Users.Any(u => u.Name == request.Name))
            {
                return (false, "Username already exists");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            _context.Users.Add(new User
            {
                Name = request.Name,
                HashedPassword = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                Active = true,
                Role = request.role,
                DepartmentName = request.DepartmentName
            });
            await _context.SaveChangesAsync();
            return (true, $"User {request.Name} created successfully");
        }

        public async Task<List<UserResponseDTO>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
            return users.Select(u => new UserResponseDTO(u)).ToList();
        }

        public async Task<PaginatedResult<UserResponseDTO>> GetAllUsersPaginatedAsync(int page, int perPage)
        {
            var query = _context.Users
                .Select(u => new UserResponseDTO
                {
                    Id = u.Id,
                    Name = u.Name,
                    role = u.Role,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin,
                    Active = u.Active,
                    DepartmentName = u.DepartmentName
                });

            return await PaginatedResult<UserResponseDTO>.CreateAsync(query, page, perPage);
        }

        public async Task<UserResponseDTO?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;
            return new UserResponseDTO(user);
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(int id, UserUpdateDTO request, bool isAdmin)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return (false, "User not found");
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                user.Name = request.Name;
            }
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            if (request.Active.ToString() != "" && isAdmin)
            {
                user.Active = request.Active;
            }
            if (request.role.ToString() != "" && isAdmin)
            {
                user.Role = request.role;
            }
            if (!string.IsNullOrEmpty(request.DepartmentName) && isAdmin)
            {
                user.DepartmentName = request.DepartmentName;
            }

            await _context.SaveChangesAsync();
            return (true, $"User {user.Name} updated successfully");
        }

        public async Task<(bool Success, string Message)> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return (false, "User not found");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return (true, $"User {user.Name} deleted successfully");
        }

        public async Task<PaginatedResult<UserResponseDTO>> SearchUsersByNameAsync(string name, int page, int perPage)
        {
            var query = _context.Users
                .Where(u => u.Name.ToLower().Contains(name.ToLower()))
                .Select(u => new UserResponseDTO
                {
                    Id = u.Id,
                    Name = u.Name,
                    role = u.Role,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin,
                    Active = u.Active,
                    DepartmentName = u.DepartmentName
                });

            return await PaginatedResult<UserResponseDTO>.CreateAsync(query, page, perPage);
        }
    }
}
