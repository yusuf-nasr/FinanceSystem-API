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
            if (await _context.Users.AnyAsync(u => u.Name == request.Name))
                throw new ApiException(409, ErrorCode.USER_ALREADY_EXISTS,
                    new Dictionary<string, object> { { "userName", request.Name } });

            if (!string.IsNullOrEmpty(request.DepartmentName))
            {
                var deptExists = await _context.Departments.AnyAsync(d => d.Name == request.DepartmentName);
                if (!deptExists)
                    throw new ApiException(404, ErrorCode.DEPARTMENT_NOT_FOUND,
                        new Dictionary<string, object> { { "departmentName", request.DepartmentName } });
            }

            var hashedPassword = Isopoh.Cryptography.Argon2.Argon2.Hash(request.Password);
            var user = new User
            {
                Name = request.Name,
                HashedPassword = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                Active = true,
                Role = request.Role,
                Presence = UserPresence.OFFLINE,
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
                Role = u.Role,
                Presence = u.Presence,
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
                    new Dictionary<string, object> { { "userId", id.ToString() } });

            if (request.Name != null && request.Name != user.Name)
            {
                if (await _context.Users.AnyAsync(u => u.Name == request.Name))
                    throw new ApiException(409, ErrorCode.USER_ALREADY_EXISTS,
                        new Dictionary<string, object> { { "userName", request.Name } });
                user.Name = request.Name;
            }
            if (request.Password != null)
                user.HashedPassword = Isopoh.Cryptography.Argon2.Argon2.Hash(request.Password);
            if (request.Active.HasValue) user.Active = request.Active.Value;
            if (request.Role.HasValue) user.Role = request.Role.Value;
            if (request.DepartmentName != null)
            {
                var deptExists = await _context.Departments.AnyAsync(d => d.Name == request.DepartmentName);
                if (!deptExists)
                    throw new ApiException(404, ErrorCode.DEPARTMENT_NOT_FOUND,
                        new Dictionary<string, object> { { "departmentName", request.DepartmentName } });
                user.DepartmentName = request.DepartmentName;
            }

            await _context.SaveChangesAsync();
            return new UserResponseDTO(user);
        }

        public async Task<UserResponseDTO> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new ApiException(404, ErrorCode.USER_NOT_FOUND,
                    new Dictionary<string, object> { { "userId", id.ToString() } });
 
            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ApiException(409, ErrorCode.USER_ENGAGED_IN_SYSTEM,
                    new Dictionary<string, object> { { "userId", id.ToString() } });
            }

            return new UserResponseDTO(user);
        }

        public async Task UpdatePresenceAsync(int userId, UserPresence presence)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Presence = presence;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ResetAllPresenceAsync()
        {
            await _context.Users
                .Where(u => u.Presence == UserPresence.ONLINE)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Presence, UserPresence.OFFLINE));
        }
    }
}
