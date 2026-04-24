using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
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

        public async Task<UserResponseDTO> CreateUserAsync(UserCreateDTO request)
        {
            if (_context.Users.Any(u => u.Name == request.Name))
                throw new ApiException(409, ErrorCode.USER_ALREADY_EXISTS,
                    new Dictionary<string, object> { { "name", request.Name } });

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User
            {
                Name = request.Name,
                HashedPassword = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                Active = true,
                Role = request.role,
                DepartmentName = request.DepartmentName
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return new UserResponseDTO(user);
        }

        public async Task<PaginatedResult<UserResponseDTO>> FindAllAsync(UserQueryDTO query)
        {
            // Default: only active users (unless admin explicitly filters)
            IQueryable<User> q = _context.Users.Where(u => u.Active);

            if (query.Active.HasValue)
                q = _context.Users.Where(u => u.Active == query.Active.Value);

            if (!string.IsNullOrWhiteSpace(query.Name))
                q = q.Where(u => u.Name.ToLower().Contains(query.Name.ToLower()));

            if (!string.IsNullOrWhiteSpace(query.Department))
                q = q.Where(u => u.DepartmentName != null &&
                    u.DepartmentName.ToLower().Contains(query.Department.ToLower()));

            if (query.Role.HasValue)
                q = q.Where(u => u.Role == query.Role.Value);

            var projected = q.Select(u => new UserResponseDTO
            {
                Id = u.Id,
                Name = u.Name,
                role = u.Role,
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin,
                Active = u.Active,
                DepartmentName = u.DepartmentName
            });

            return await PaginatedResult<UserResponseDTO>.CreateAsync(projected, query.Page, query.PerPage);
        }

        public async Task<UserResponseDTO?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;
            return new UserResponseDTO(user);
        }

        public async Task<UserResponseDTO> UpdateUserAsync(int id, UserUpdateDTO request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new ApiException(404, ErrorCode.USER_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });

            if (request.Name != null) user.Name = request.Name;
            if (request.Password != null)
                user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            if (request.Active.HasValue) user.Active = request.Active.Value;
            if (request.role.HasValue) user.Role = request.role.Value;
            if (request.DepartmentName != null) user.DepartmentName = request.DepartmentName;

            await _context.SaveChangesAsync();
            return new UserResponseDTO(user);
        }

        public async Task<UserResponseDTO> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new ApiException(404, ErrorCode.USER_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ApiException(409, ErrorCode.USER_ENGAGED_IN_SYSTEM,
                    new Dictionary<string, object> { { "id", id } });
            }

            return new UserResponseDTO(user);
        }
    }
}
