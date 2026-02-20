using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
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

        public async Task<(bool Success, string Message)> CreateDepartmentAsync(DeptCreateDTO request)
        {
            if (await _context.Departments.AnyAsync(d => d.Name == request.Name))
            {
                return (false, "Department name already exists.");
            }

            await _context.Departments.AddAsync(new Department
            {
                Name = request.Name,
                ManagerId = request.ManagerId.HasValue ? request.ManagerId.Value : null
            });
            await _context.SaveChangesAsync();
            return (true, "Department created successfully.");
        }

        public async Task<List<DeptResponseDTO>> GetAllDepartmentsAsync()
        {
            return await _context.Departments.Select(d => new DeptResponseDTO
            {
                Name = d.Name,
                ManagerId = d.ManagerId
            }).ToListAsync();
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

        public async Task<(bool Success, string Message)> UpdateDepartmentAsync(string name, DeptUpdateDTO request)
        {
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);
            if (department == null)
            {
                return (false, "Department not found.");
            }

            bool isRenaming = request.Name is not null && name != request.Name;

            if (isRenaming)
            {
                if (await _context.Departments.AnyAsync(d => d.Name == request.Name))
                {
                    return (false, "Department name already exists.");
                }

                int? newManagerId = request.ManagerId is int mid ? mid : department.ManagerId;
                var usersInDept = await _context.Users.Where(u => u.DepartmentName == name).ToListAsync();

                await _context.Departments.AddAsync(new Department
                {
                    Name = request.Name,
                    ManagerId = newManagerId
                });
                foreach (var user in usersInDept)
                {
                    user.DepartmentName = request.Name;
                }
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (request.ManagerId is int id)
                {
                    department.ManagerId = id;
                }
            }

            await _context.SaveChangesAsync();
            return (true, "Department updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteDepartmentAsync(string name)
        {
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);
            if (department == null)
            {
                return (false, "Department not found.");
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            return (true, "Department deleted successfully.");
        }
    }
}
