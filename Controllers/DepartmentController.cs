using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly FinanceDbContext _context;
        private readonly IFinanceService services;

        public DepartmentController(FinanceDbContext context, IFinanceService services)
        {
            _context = context;
            this.services = services;
        }
        [HttpPost] 
        [Authorize]
        public async Task<IActionResult> CreateDepartment(DeptCreateDTO request)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!services.IsAdmin(UID))
            {
                return Forbid("Only admins can create departments.");
            }
            if (await _context.Departments.AnyAsync(d => d.Name == request.Name))
            {
                return BadRequest("Department name already exists.");
            }
            await _context.Departments.AddAsync(new Models.Department
            {
                Name = request.Name,
                ManagerId = request.ManagerId.HasValue ? request.ManagerId.Value : null
            });
            await _context.SaveChangesAsync();
            return Ok("Department created successfully.");
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _context.Departments.Select(d => new DeptResponseDTO
            {
                Name = d.Name,
                ManagerId = d.ManagerId
            }).ToListAsync();
            return Ok(departments);
        }
        [HttpGet("{name}")]
        [Authorize]
        public async Task<IActionResult> GetDepartmentByName(string name)
        {
            var department = await _context.Departments.Where(d => d.Name == name).Select(d => new DeptResponseDTO
            {
                Name = d.Name,
                ManagerId = d.ManagerId
            }).FirstOrDefaultAsync();
            if (department == null)
            {
                return NotFound("Department not found.");
            }
            return Ok(department);
        }
        [HttpPatch("{name}")]
        [Authorize]
        public async Task<IActionResult> UpdateDepartment(string name, [FromBody] DeptUpdateDTO request)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!(services.IsAdmin(UID) || services.IsManager(name, UID)))
            {
                return Forbid("Only admins and managers can update departments.");
            }
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);
            if (department == null)
            {
                return NotFound("Department not found.");
            }

            bool isRenaming = request.Name is not null && name != request.Name;

            if (isRenaming)
            {
                if (await _context.Departments.AnyAsync(d => d.Name == request.Name))
                {
                    return BadRequest("Department name already exists.");
                }

                int? newManagerId = request.ManagerId is int mid ? mid : department.ManagerId;
                var usersInDept = await _context.Users.Where(u => u.DepartmentName == name).ToListAsync();
                
                await _context.Departments.AddAsync(new Models.Department
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
                // No rename — just update ManagerId if provided
                if (request.ManagerId is int id)
                {
                    department.ManagerId = id;
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Department updated successfully.");
        }
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteDepartment(string name)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!services.IsAdmin(UID))
            {
                return Forbid("Only admins can delete departments.");
            }
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);
            if (department == null)
            {
                return NotFound("Department not found.");
            }
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            return Ok("Department deleted successfully.");
        }
    }
}