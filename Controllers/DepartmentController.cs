using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/departments")]
    [ApiController]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        private Role GetCurrentUserRole()
        {
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<Role>(roleStr, out var role) ? role : Role.USER;
        }

        // POST /api/v1/departments — Admin only
        [HttpPost]
        public async Task<IActionResult> CreateDepartment(DeptCreateDTO request)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE);

            var result = await _departmentService.CreateDepartmentAsync(request);
            if (!result.Success)
                return BadRequest(result.Message);
            return StatusCode(201, result.Message);
        }

        // GET /api/v1/departments
        [HttpGet]
        public async Task<IActionResult> GetDepartments([FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            var paginated = await _departmentService.GetAllDepartmentsPaginatedAsync(page, perPage);
            return Ok(paginated);
        }

        // GET /api/v1/departments/:name
        [HttpGet("{name}")]
        public async Task<IActionResult> GetDepartmentByName(string name)
        {
            var department = await _departmentService.GetDepartmentByNameAsync(name);
            if (department == null)
                throw new ApiException(404, ErrorCode.DEPARTMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "name", name } });
            return Ok(department);
        }

        // PATCH /api/v1/departments/:name — Admin only (Node version does not allow manager updates)
        [HttpPatch("{name}")]
        public async Task<IActionResult> UpdateDepartment(string name, [FromBody] DeptUpdateDTO request)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE);

            var result = await _departmentService.UpdateDepartmentAsync(name, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    throw new ApiException(404, ErrorCode.DEPARTMENT_NOT_FOUND,
                        new Dictionary<string, object> { { "name", name } });
                return BadRequest(result.Message);
            }
            return Ok(result.Message);
        }

        // DELETE /api/v1/departments/:name — Admin only
        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteDepartment(string name)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE);

            var result = await _departmentService.DeleteDepartmentAsync(name);
            if (!result.Success)
                throw new ApiException(404, ErrorCode.DEPARTMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "name", name } });
            return Ok(result.Message);
        }
    }
}