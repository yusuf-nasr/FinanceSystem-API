using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/transactions/types")]
    [ApiController]
    [Authorize]
    public class TransactionTypeController : ControllerBase
    {
        private readonly ITransactionTypeService _transactionTypeService;

        public TransactionTypeController(ITransactionTypeService transactionTypeService)
        {
            _transactionTypeService = transactionTypeService;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        private Role GetCurrentUserRole()
        {
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<Role>(roleStr, out var role) ? role : Role.USER;
        }

        // POST /api/v1/transactions/types
        [HttpPost]
        public async Task<IActionResult> CreateTransactionType(TransactionTypeCreateDTO request)
        {
            var creatorId = GetCurrentUserId();
            var result = await _transactionTypeService.CreateAsync(request, creatorId);
            return StatusCode(201, result);
        }

        // GET /api/v1/transactions/types
        [HttpGet]
        public async Task<IActionResult> GetTransactionTypes([FromQuery] TransactionTypeQueryDTO query)
        {
            var role = GetCurrentUserRole();

            // Only admin can filter by creatorId
            if (query.CreatorId.HasValue && role != Role.ADMIN)
                throw new ApiException(403, ErrorCode.RESTRICTED_FIELD_UPDATE,
                    new Dictionary<string, object> { { "fields", "creatorId" } });

            var paginated = await _transactionTypeService.FindAllAsync(query);
            return Ok(paginated);
        }

        // GET /api/v1/transactions/types/:name
        [HttpGet("{name}")]
        public async Task<IActionResult> GetTransactionTypeByName(string name)
        {
            var result = await _transactionTypeService.GetByNameAsync(name);
            if (result == null)
                throw new ApiException(404, ErrorCode.TRANSACTION_TYPE_NOT_FOUND,
                    new Dictionary<string, object> { { "name", name } });
            return Ok(result);
        }

        // DELETE /api/v1/transactions/types/:name
        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteTransactionType(string name)
        {
            var userId = GetCurrentUserId();
            var isAdmin = GetCurrentUserRole() == Role.ADMIN;

            var result = await _transactionTypeService.DeleteAsync(name, userId, isAdmin);
            return Ok(result);
        }
    }
}
