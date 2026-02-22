using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/transaction/{transactionId}/forward")]
    [ApiController]
    [Authorize]
    public class TransactionForwardController : ControllerBase
    {
        private readonly ITransactionForwardService _forwardService;

        public TransactionForwardController(ITransactionForwardService forwardService)
        {
            _forwardService = forwardService;
        }

        [HttpPost]
        public async Task<ActionResult<TransactionForwardDTO>> Create(int transactionId, TransactionForwardCreateDTO dto)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _forwardService.CreateAsync(transactionId, dto, senderId);
            return CreatedAtAction(nameof(FindOne), new { transactionId, id = result.Id }, result);
        }

        [HttpGet]
        public async Task<ActionResult> FindAll(int transactionId, [FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            var result = await _forwardService.FindAllPaginatedAsync(transactionId, page, perPage);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionForwardDTO>> FindOne(int transactionId, int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _forwardService.FindOneAsync(transactionId, id);
            if (result == null) return NotFound();

            // Mark as seen
            await _forwardService.MarkAsSeenAsync(transactionId, id, userId);

            return Ok(result);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<TransactionForwardDTO>> UpdateSender(int transactionId, int id, TransactionForwardSenderUpdateDTO dto)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _forwardService.UpdateSenderAsync(transactionId, id, dto, senderId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/response")]
        public async Task<ActionResult<TransactionForwardDTO>> Respond(int transactionId, int id, TransactionForwardUpdateDTO dto)
        {
            var receiverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _forwardService.RespondAsync(transactionId, id, dto, receiverId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPatch("{id}/response")]
        public async Task<ActionResult<TransactionForwardDTO>> UpdateResponse(int transactionId, int id, TransactionForwardUpdateDTO dto)
        {
            var receiverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _forwardService.UpdateResponseAsync(transactionId, id, dto, receiverId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPatch("{id}/sender-comment")]
        public async Task<ActionResult<TransactionForwardDTO>> EditSenderComment(int transactionId, int id, TransactionForwardSenderUpdateDTO dto)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _forwardService.EditSenderCommentAsync(transactionId, id, dto.Comment, senderId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPatch("{id}/receiver-comment")]
        public async Task<ActionResult<TransactionForwardDTO>> EditReceiverComment(int transactionId, int id, TransactionForwardSenderUpdateDTO dto)
        {
            var receiverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _forwardService.EditReceiverCommentAsync(transactionId, id, dto.Comment, receiverId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<TransactionForwardDTO>> Remove(int transactionId, int id)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _forwardService.DeleteAsync(transactionId, id, senderId);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}
