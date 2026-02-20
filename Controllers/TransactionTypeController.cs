using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/transactions/types")]
    [ApiController]
    public class TransactionTypeController : ControllerBase
    {
        private readonly ITransactionTypeService _transactionTypeService;
        private readonly IFinanceService _financeService;

        public TransactionTypeController(ITransactionTypeService transactionTypeService, IFinanceService financeService)
        {
            _transactionTypeService = transactionTypeService;
            _financeService = financeService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTransactionType(TransactionTypeCreateDTO request)
        {
            var creatorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _transactionTypeService.CreateAsync(request, creatorId);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Message);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetTransactionTypes([FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            var result = await _transactionTypeService.GetAllAsync();
            var paginated = PaginatedResult<TransactionTypeResponseDTO>.Create(result, page, perPage);
            return Ok(paginated);
        }

        [HttpGet("{name}")]
        [Authorize]
        public async Task<IActionResult> GetTransactionTypeByName(string name)
        {
            var result = await _transactionTypeService.GetByNameAsync(name);
            if (result == null)
            {
                return NotFound("Transaction type not found.");
            }
            return Ok(result);
        }

        [HttpDelete("{name}")]
        [Authorize]
        public async Task<IActionResult> DeleteTransactionType(string name)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Check permission before deleting — need to look up the type first
            var transactionType = await _transactionTypeService.GetByNameAsync(name);
            if (transactionType == null)
            {
                return NotFound("Transaction type not found.");
            }

            if (!(_financeService.IsAdmin(UID) || transactionType.CreatorId == UID))
            {
                return Forbid("Only admins or creator can delete transaction types.");
            }

            var result = await _transactionTypeService.DeleteAsync(name);
            if (!result.Success)
            {
                return NotFound(result.Message);
            }
            return Ok(result.Message);
        }
    }
}
