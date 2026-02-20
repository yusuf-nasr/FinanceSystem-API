using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
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

            var result = await _transactionService.FindAllAsync(query, userId, isAdmin, page, perPage);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<TransactionDTO>> FindOne(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var isAdmin = _financeService.IsAdmin(userId);

            var result = await _transactionService.FindOneAsync(id);
            if (result == null)
                return NotFound();

            // Check participant permission (admin can always view)
            if (!isAdmin && !await _transactionService.IsParticipant(id, userId))
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_PARTICIPANT);

            // Mark as seen
            await _transactionService.MarkAsSeenAsync(id, userId);

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
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_CREATOR);
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
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_CREATOR);
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

            // Check participant permission
            if (!await _transactionService.IsParticipant(id, userId))
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_PARTICIPANT);

            // Check forward restriction (can't edit if forward already seen/responded)
            await _transactionService.CheckRestriction(id, userId);

            var result = await _transactionService.AttachDocumentAsync(id, documentId, userId);
            return Ok(result);
        }

        [HttpDelete("{id}/document/{documentId}")]
        [Authorize]
        public async Task<ActionResult<TransactionDTO>> DetachDocument(int id, int documentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Check participant permission
            if (!await _transactionService.IsParticipant(id, userId))
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_PARTICIPANT);

            // Check forward restriction
            await _transactionService.CheckRestriction(id, userId);

            var result = await _transactionService.DetachDocumentAsync(id, documentId, userId);
            return Ok(result);
        }
    }
}
