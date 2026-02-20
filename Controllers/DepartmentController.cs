using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/departments")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;
        private readonly IFinanceService _financeService;

        public DepartmentController(IDepartmentService departmentService, IFinanceService financeService)
        {
            _departmentService = departmentService;
            _financeService = financeService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateDepartment(DeptCreateDTO request)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!_financeService.IsAdmin(UID))
            {
                return Forbid("Only admins can create departments.");
            }

            var result = await _departmentService.CreateDepartmentAsync(request);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Message);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetDepartments([FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            var paginated = await _departmentService.GetAllDepartmentsPaginatedAsync(page, perPage);
            return Ok(paginated);
        }

        [HttpGet("{name}")]
        [Authorize]
        public async Task<IActionResult> GetDepartmentByName(string name)
        {
            var department = await _departmentService.GetDepartmentByNameAsync(name);
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
            if (!(_financeService.IsAdmin(UID) || _financeService.IsManager(name, UID)))
            {
                return Forbid("Only admins and managers can update departments.");
            }

            var result = await _departmentService.UpdateDepartmentAsync(name, request);
            if (!result.Success)
            {
                // Distinguish between not found and conflict
                if (result.Message.Contains("not found"))
                    return NotFound(result.Message);
                return BadRequest(result.Message);
            }
            return Ok(result.Message);
        }

        [HttpDelete("{name}")]
        [Authorize]
        public async Task<IActionResult> DeleteDepartment(string name)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!_financeService.IsAdmin(UID))
            {
                return Forbid("Only admins can delete departments.");
            }

            var result = await _departmentService.DeleteDepartmentAsync(name);
            if (!result.Success)
            {
                return NotFound(result.Message);
            }
            return Ok(result.Message);
        }
    }
}