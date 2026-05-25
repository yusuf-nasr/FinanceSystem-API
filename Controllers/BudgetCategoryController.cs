using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v0/budget-categories")]
    [ApiController]
    [Authorize]
    public class BudgetCategoryController : ControllerBase
    {
        private readonly IBudgetCategoryService _budgetCategoryService;

        public BudgetCategoryController(IBudgetCategoryService budgetCategoryService)
        {
            _budgetCategoryService = budgetCategoryService;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        private Role GetCurrentUserRole()
        {
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<Role>(roleStr, out var role) ? role : Role.USER;
        }

        // POST /api/v1/budget-categories/{name} — Admin only
        [HttpPost("{name}")]
        public async Task<ActionResult<object>> Create(string name)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE,
                    new Dictionary<string, object> { { "roles", "ADMIN" } });

            var result = await _budgetCategoryService.CreateAsync(name);
            return StatusCode(201, result);
        }

        // GET /api/v1/budget-categories
        [HttpGet]
        public async Task<ActionResult> FindAll([FromQuery] BudgetCategoryQueryDTO query)
        {
            var role = GetCurrentUserRole();
            var result = await _budgetCategoryService.FindAllAsync(query, role);
            return Ok(result);
        }

        // GET /api/v1/budget-categories/{name}
        [HttpGet("{name}")]
        public async Task<ActionResult> FindOne(string name)
        {
            var role = GetCurrentUserRole();
            var result = await _budgetCategoryService.FindOneAsync(name, role);
            return Ok(result);
        }

        // PATCH /api/v1/budget-categories/{name} — Admin only
        [HttpPatch("{name}")]
        public async Task<ActionResult<object>> Update(string name, UpdateBudgetCategoryDTO dto)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE,
                    new Dictionary<string, object> { { "roles", "ADMIN" } });

            var result = await _budgetCategoryService.UpdateAsync(name, dto);
            return Ok(result);
        }

        // DELETE /api/v1/budget-categories/{name} — Admin only
        [HttpDelete("{name}")]
        public async Task<ActionResult<object>> Delete(string name)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE,
                    new Dictionary<string, object> { { "roles", "ADMIN" } });

            var role = GetCurrentUserRole();
            var result = await _budgetCategoryService.DeleteAsync(name, role);
            return Ok(result);
        }

        // GET /api/v1/budget-categories/{name}/entry
        [HttpGet("{name}/entry")]
        public async Task<ActionResult> FindAllEntries(string name, [FromQuery] BudgetEntryQueryDTO query)
        {
            var result = await _budgetCategoryService.FindAllEntriesAsync(name, query);
            return Ok(result);
        }

        // POST /api/v1/budget-categories/{name}/entry — Admin only
        [HttpPost("{name}/entry")]
        public async Task<ActionResult<BudgetEntryDTO>> AddEntry(string name, CreateBudgetEntryDTO dto)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE,
                    new Dictionary<string, object> { { "roles", "ADMIN" } });

            var userId = GetCurrentUserId();
            var result = await _budgetCategoryService.AddEntryAsync(name, dto, userId);
            return StatusCode(201, result);
        }

        // DELETE /api/v1/budget-categories/{name}/entry/{id} — Admin only
        [HttpDelete("{name}/entry/{id}")]
        public async Task<ActionResult<BudgetEntryDTO>> RemoveEntry(string name, int id)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE,
                    new Dictionary<string, object> { { "roles", "ADMIN" } });

            var result = await _budgetCategoryService.RemoveEntryAsync(name, id);
            return Ok(result);
        }
    }
}
