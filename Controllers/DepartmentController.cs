using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v0/departments")]
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
        public async Task<ActionResult<DeptResponseDTO>> CreateDepartment(DeptCreateDTO request)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE,
                    new Dictionary<string, object> { { "roles", "ADMIN" } });

            var result = await _departmentService.CreateDepartmentAsync(request);
            return StatusCode(201, result);
        }

        // GET /api/v1/departments
        [HttpGet]
        public async Task<ActionResult> GetDepartments([FromQuery] DeptQueryDTO query)
        {
            var paginated = await _departmentService.GetAllDepartmentsPaginatedAsync(query);
            return Ok(paginated);
        }

        // GET /api/v1/departments/:name
        [HttpGet("{name}")]
        public async Task<ActionResult<DeptResponseDTO>> GetDepartmentByName(string name)
        {
            var department = await _departmentService.GetDepartmentByNameAsync(name);
            if (department == null)
                throw new ApiException(404, ErrorCode.DEPARTMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "departmentName", name } });
            return Ok(department);
        }

        // PATCH /api/v1/departments/:name — Admin only
        [HttpPatch("{name}")]
        public async Task<ActionResult<DeptResponseDTO>> UpdateDepartment(string name, [FromBody] DeptUpdateDTO request)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE,
                    new Dictionary<string, object> { { "roles", "ADMIN" } });

            var result = await _departmentService.UpdateDepartmentAsync(name, request);
            return Ok(result);
        }

        // DELETE /api/v1/departments/:name — Admin only
        [HttpDelete("{name}")]
        public async Task<ActionResult<DeptResponseDTO>> DeleteDepartment(string name)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE,
                    new Dictionary<string, object> { { "roles", "ADMIN" } });

            var result = await _departmentService.DeleteDepartmentAsync(name);
            return Ok(result);
        }
    }
}