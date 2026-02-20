using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/transactions")]
    [ApiController]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IFinanceService _financeService;

        public TransactionController(ITransactionService transactionService, IFinanceService financeService)
        {
            _transactionService = transactionService;
            _financeService = financeService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TransactionDTO>> Create(TransactionCreateDTO dto)
        {
            var creatorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _transactionService.CreateAsync(dto, creatorId);
            return CreatedAtAction(nameof(FindOne), new { id = result.Id }, result);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> FindAll([FromQuery] TransactionQuery? query, [FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var isAdmin = _financeService.IsAdmin(userId);

            var result = await _transactionService.FindAllAsync(query, userId, isAdmin);
            if (result == null)
                return Forbid();

            var paginated = PaginatedResult<TransactionDTO>.Create(result, page, perPage);
            return Ok(paginated);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<TransactionDTO>> FindOne(int id)
        {
            var result = await _transactionService.FindOneAsync(id);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<ActionResult<TransactionDTO>> Update(int id, TransactionUpdateDTO dto)
        {
            var creatorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Check permission: must be admin or creator
            var transaction = await _transactionService.FindOneAsync(id);
            if (transaction == null) return NotFound();

            if (!(_financeService.IsAdmin(creatorId) || transaction.CreatorId == creatorId))
            {
                return Forbid("only admins or creator can update transaction");
            }

            var result = await _transactionService.UpdateAsync(id, dto, creatorId);
            if (result == null) return NotFound();

            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<TransactionDTO>> Remove(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Check permission: must be admin or creator
            var transaction = await _transactionService.FindOneAsync(id);
            if (transaction == null) return NotFound();

            if (!(_financeService.IsAdmin(userId) || transaction.CreatorId == userId))
            {
                return Forbid("only admins or creator can delete transaction");
            }

            var result = await _transactionService.DeleteAsync(id);
            if (result == null) return NotFound();

            return Ok(result);
        }

        [HttpPost("{id}/document/{documentId}")]
        [Authorize]
        public async Task<ActionResult<TransactionDTO>> AttachDocument(int id, int documentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _transactionService.AttachDocumentAsync(id, documentId, userId);
            return Ok(result);
        }

        [HttpDelete("{id}/document/{documentId}")]
        [Authorize]
        public async Task<ActionResult<TransactionDTO>> DetachDocument(int id, int documentId)
        {
            var result = await _transactionService.DetachDocumentAsync(id, documentId);
            return Ok(result);
        }
    }
}
