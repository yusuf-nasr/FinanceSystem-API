using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Models;
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

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        private Role GetCurrentUserRole()
        {
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<Role>(roleStr, out var role) ? role : Role.USER;
        }

        // POST /api/v1/transactions
        [HttpPost]
        public async Task<ActionResult<TransactionDTO>> Create(TransactionCreateDTO dto)
        {
            var creatorId = GetCurrentUserId();
            var result = await _transactionService.CreateAsync(dto, creatorId);
            return CreatedAtAction(nameof(FindOne), new { id = result.Id }, result);
        }

        // GET /api/v1/transactions
        [HttpGet]
        public async Task<ActionResult> FindAll([FromQuery] TransactionQuery? query, [FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var isAdmin = role == Role.ADMIN;

            if (query == TransactionQuery.All && !isAdmin)
                throw new ApiException(403, ErrorCode.MISSING_ROLE);

            var result = await _transactionService.FindAllAsync(query, userId, isAdmin, page, perPage);
            return Ok(result);
        }

        // GET /api/v1/transactions/:id
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDTO>> FindOne(int id)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var isAdmin = role == Role.ADMIN;

            var result = await _transactionService.FindOneAsync(id);
            if (result == null)
                throw new ApiException(404, ErrorCode.TRANSACTION_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });

            if (!isAdmin && !await _transactionService.IsParticipant(id, userId))
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_PARTICIPANT,
                    new Dictionary<string, object> { { "transactionId", id } });

            await _transactionService.MarkAsSeenAsync(id, userId);

            return Ok(result);
        }

        // PATCH /api/v1/transactions/:id
        [HttpPatch("{id}")]
        public async Task<ActionResult<TransactionDTO>> Update(int id, TransactionUpdateDTO dto)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var isAdmin = role == Role.ADMIN;
            var isAccountant = role == Role.ACCOUNTANT;

            bool updatingFulfilled = dto.Fulfilled.HasValue;
            bool updatingBudgetFields = dto.BudgetName != null || dto.BudgetAllocation.HasValue;
            bool updatingNonAccountantFields = dto.Title != null || dto.Description != null ||
                                               dto.TransactionTypeName != null || dto.Priority.HasValue;

            // Only accountant or admin can set fulfilled/budget fields
            if ((updatingFulfilled || updatingBudgetFields) && !isAdmin && !isAccountant)
                throw new ApiException(403, ErrorCode.RESTRICTED_FIELD_UPDATE,
                    new Dictionary<string, object> { { "fields", "fulfilled, budgetName, budgetAllocation" } });

            // Validate: if setting fulfilled=true, budgetName + budgetAllocation are required
            if (dto.Fulfilled == true && (!updatingBudgetFields || string.IsNullOrEmpty(dto.BudgetName) || !dto.BudgetAllocation.HasValue))
                throw new ApiException(400, ErrorCode.MISSING_BUDGET_INFO,
                    new Dictionary<string, object> { { "required", "budgetName, budgetAllocation" } });

            // Non-admin, non-accountant updates: must be the creator
            bool isUpdatingOnlyAccountantFields = (updatingFulfilled || updatingBudgetFields) && !updatingNonAccountantFields;
            if (!isAdmin && !(isAccountant && isUpdatingOnlyAccountantFields))
            {
                if (!await _transactionService.IsCreator(id, userId))
                    throw new ApiException(403, ErrorCode.NOT_TRANSACTION_CREATOR,
                        new Dictionary<string, object> { { "transactionId", id } });
            }

            var result = await _transactionService.UpdateAsync(id, dto, userId, role);
            if (result == null)
                throw new ApiException(404, ErrorCode.TRANSACTION_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });

            return Ok(result);
        }

        // DELETE /api/v1/transactions/:id
        [HttpDelete("{id}")]
        public async Task<ActionResult<TransactionDTO>> Remove(int id)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            var isAdmin = role == Role.ADMIN;

            if (!isAdmin && !await _transactionService.IsCreator(id, userId))
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_CREATOR,
                    new Dictionary<string, object> { { "transactionId", id } });

            var result = await _transactionService.DeleteAsync(id, role);
            if (result == null)
                throw new ApiException(404, ErrorCode.TRANSACTION_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });

            return Ok(result);
        }

        // POST /api/v1/transactions/:id/document/:documentId
        [HttpPost("{id}/document/{documentId}")]
        public async Task<ActionResult<TransactionDTO>> AttachDocument(int id, int documentId)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            if (role != Role.ADMIN)
                await CheckRestriction(userId, id);

            var result = await _transactionService.AttachDocumentAsync(id, documentId, userId);
            return Ok(result);
        }

        // DELETE /api/v1/transactions/:id/document/:documentId
        [HttpDelete("{id}/document/{documentId}")]
        public async Task<ActionResult<TransactionDTO>> DetachDocument(int id, int documentId)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            if (role != Role.ADMIN)
            {
                await CheckRestriction(userId, id);

                if (!await _transactionService.IsAttacher(id, documentId, userId))
                    throw new ApiException(403, ErrorCode.NOT_DOCUMENT_ATTACHER,
                        new Dictionary<string, object> { { "transactionId", id }, { "documentId", documentId } });
            }

            var result = await _transactionService.DetachDocumentAsync(id, documentId, userId, role);
            return Ok(result);
        }

        /// <summary>
        /// Mirrors Node's private checkRestriction():
        /// - If user is the latest forward sender: block if receiver already seen it
        /// - If user is the latest forward receiver: block if already responded
        /// - Otherwise: user is not a participant → NOT_TRANSACTION_PARTICIPANT
        /// </summary>
        private async Task CheckRestriction(int userId, int transactionId)
        {
            var latestForward = await _transactionService.FindLatestForward(transactionId);

            if (latestForward == null) return;

            if (latestForward.SenderId == userId)
            {
                if (latestForward.ReceiverSeen)
                    throw new ApiException(403, ErrorCode.FORWARD_ALREADY_SEEN,
                        new Dictionary<string, object> { { "transactionId", transactionId } });
            }
            else if (latestForward.ReceiverId == userId)
            {
                if (latestForward.Status != TransactionForwardStatus.WAITING)
                    throw new ApiException(403, ErrorCode.FORWARD_ALREADY_RESPONDED,
                        new Dictionary<string, object> { { "transactionId", transactionId } });
            }
            else
            {
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_PARTICIPANT,
                    new Dictionary<string, object> { { "transactionId", transactionId } });
            }
        }
    }
}
