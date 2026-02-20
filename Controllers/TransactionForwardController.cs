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
            var result = await _forwardService.FindAllAsync(transactionId);
            var paginated = PaginatedResult<TransactionForwardDTO>.Create(result, page, perPage);
            return Ok(paginated);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionForwardDTO>> FindOne(int transactionId, int id)
        {
            var result = await _forwardService.FindOneAsync(transactionId, id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<TransactionForwardDTO>> UpdateSender(int transactionId, int id, TransactionForwardSenderUpdateDTO dto)
        {
            var result = await _forwardService.UpdateSenderAsync(transactionId, id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/response")]
        public async Task<ActionResult<TransactionForwardDTO>> Respond(int transactionId, int id, TransactionForwardUpdateDTO dto)
        {
            var result = await _forwardService.RespondAsync(transactionId, id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPatch("{id}/response")]
        public async Task<ActionResult<TransactionForwardDTO>> UpdateResponse(int transactionId, int id, TransactionForwardUpdateDTO dto)
        {
            var result = await _forwardService.UpdateResponseAsync(transactionId, id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<TransactionForwardDTO>> Remove(int transactionId, int id)
        {
            var result = await _forwardService.DeleteAsync(transactionId, id);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}
