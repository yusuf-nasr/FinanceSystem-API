using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceSystem_Dotnet.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly FinanceDbContext _context;

        public DepartmentService(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<DeptResponseDTO> CreateDepartmentAsync(DeptCreateDTO request)
        {
            if (await _context.Departments.AnyAsync(d => d.Name == request.Name))
                throw new ApiException(409, ErrorCode.DEPARTMENT_ALREADY_EXISTS,
                    new Dictionary<string, object> { { "departmentName", request.Name } });

            // Validate manager if provided
            if (request.ManagerId.HasValue)
            {
                var manager = await _context.Users.FindAsync(request.ManagerId.Value);
                if (manager == null)
                    throw new ApiException(404, ErrorCode.MANAGER_NOT_FOUND,
                        new Dictionary<string, object> { { "managerId", request.ManagerId.Value.ToString() } });

                // Check if manager already manages another department
                var existingDept = await _context.Departments
                    .FirstOrDefaultAsync(d => d.ManagerId == request.ManagerId.Value);
                if (existingDept != null)
                {
                    throw new ApiException(409, ErrorCode.MANAGER_ALREADY_MANAGES_DEPARTMENT,
                        new Dictionary<string, object> { { "managerId", request.ManagerId.Value.ToString() } });
                }
            }

            var department = new Department
            {
                Name = request.Name,
                ManagerId = request.ManagerId
            };

            await _context.Departments.AddAsync(department);
            await _context.SaveChangesAsync();

            return new DeptResponseDTO
            {
                Name = department.Name,
                ManagerId = department.ManagerId
            };
        }

        public async Task<PaginatedResult<DeptResponseDTO>> GetAllDepartmentsPaginatedAsync(DeptQueryDTO query)
        {
            var q = _context.Departments.AsQueryable();

            if (!string.IsNullOrEmpty(query.Name))
                q = q.Where(d => d.Name.ToLower().Contains(query.Name.ToLower()));

            if (!string.IsNullOrEmpty(query.Manager))
            {
                q = q.Where(d => d.ManagerId != null &&
                    d.Manager.Name.ToLower().Contains(query.Manager.ToLower()));
            }

            var projected = q.Select(d => new DeptResponseDTO
            {
                Name = d.Name,
                ManagerId = d.ManagerId
            });

            return await PaginatedResult<DeptResponseDTO>.CreateAsync(projected, query.Page, query.PerPage);
        }

        public async Task<DeptResponseDTO?> GetDepartmentByNameAsync(string name)
        {
            return await _context.Departments
                .Where(d => d.Name == name)
                .Select(d => new DeptResponseDTO
                {
                    Name = d.Name,
                    ManagerId = d.ManagerId
                }).FirstOrDefaultAsync();
        }

        public async Task<DeptResponseDTO> UpdateDepartmentAsync(string name, DeptUpdateDTO request)
        {
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);
            if (department == null)
                throw new ApiException(404, ErrorCode.DEPARTMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "departmentName", name } });

            // Validate manager if provided
            if (request.ManagerId.HasValue)
            {
                var manager = await _context.Users.FindAsync(request.ManagerId.Value);
                if (manager == null)
                    throw new ApiException(404, ErrorCode.MANAGER_NOT_FOUND,
                        new Dictionary<string, object> { { "managerId", request.ManagerId.Value.ToString() } });

                // Check manager is member of department
                if (manager.DepartmentName != name)
                    throw new ApiException(409, ErrorCode.MANAGER_NOT_MEMBER_OF_DEPARTMENT,
                        new Dictionary<string, object> { { "managerId", request.ManagerId.Value.ToString() }, { "departmentName", name } });

                // Check if manager already manages another department
                var existingDept = await _context.Departments
                    .FirstOrDefaultAsync(d => d.ManagerId == request.ManagerId.Value && d.Name != name);
                if (existingDept != null)
                {
                    throw new ApiException(409, ErrorCode.MANAGER_ALREADY_MANAGES_DEPARTMENT,
                        new Dictionary<string, object> { { "managerId", request.ManagerId.Value.ToString() } });
                }
            }

            bool isRenaming = request.Name is not null && name != request.Name;

            if (isRenaming)
            {
                if (await _context.Departments.AnyAsync(d => d.Name == request.Name))
                    throw new ApiException(409, ErrorCode.DEPARTMENT_ALREADY_EXISTS,
                        new Dictionary<string, object> { { "departmentName", request.Name! } });

                int? newManagerId = request.ManagerId ?? department.ManagerId;
                var usersInDept = await _context.Users.Where(u => u.DepartmentName == name).ToListAsync();

                await _context.Departments.AddAsync(new Department
                {
                    Name = request.Name!,
                    ManagerId = newManagerId
                });
                foreach (var user in usersInDept)
                {
                    user.DepartmentName = request.Name!;
                }
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();

                return new DeptResponseDTO
                {
                    Name = request.Name!,
                    ManagerId = newManagerId
                };
            }
            else
            {
                if (request.ManagerId.HasValue)
                {
                    department.ManagerId = request.ManagerId.Value;
                }

                await _context.SaveChangesAsync();
                return new DeptResponseDTO
                {
                    Name = department.Name,
                    ManagerId = department.ManagerId
                };
            }
        }

        public async Task<DeptResponseDTO> DeleteDepartmentAsync(string name)
        {
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);
            if (department == null)
                throw new ApiException(404, ErrorCode.DEPARTMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "departmentName", name } });

            var dto = new DeptResponseDTO
            {
                Name = department.Name,
                ManagerId = department.ManagerId
            };

            try
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ApiException(409, ErrorCode.DEPARTMENT_HAS_MEMBERS,
                    new Dictionary<string, object> { { "departmentName", name } });
            }

            return dto;
        }
    }
}
