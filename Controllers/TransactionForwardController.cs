using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/transaction/{transactionId}/forward")]
    [ApiController]
    [Authorize]
    public class TransactionForwardController : ControllerBase
    {
        private readonly FinanceDbContext _context;

        public TransactionForwardController(FinanceDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<TransactionForwardDTO>> Create(int transactionId, TransactionForwardCreateDTO dto)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var forward = new TransactionForward
            {
                TransactionId = transactionId,
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                SenderComment = dto.Comment,
                Status = TransactionForwardStatus.WAITING,
                ForwardedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SenderSeen = true,
                ReceiverSeen = false
            };

            _context.TransactionForwards.Add(forward);
            await _context.SaveChangesAsync();

            // Refetch to include navigation properties
            forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == forward.Id);

            return CreatedAtAction(nameof(FindOne), new { transactionId, id = forward.Id }, MapToDTO(forward));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionForwardDTO>>> FindAll(int transactionId)
        {
            var forwards = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Where(f => f.TransactionId == transactionId)
                .ToListAsync();

            return Ok(forwards.Select(MapToDTO));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionForwardDTO>> FindOne(int transactionId, int id)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null)
                return NotFound();

            return Ok(MapToDTO(forward));
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<TransactionForwardDTO>> UpdateSender(int transactionId, int id, TransactionForwardSenderUpdateDTO dto)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null)
                return NotFound();

            forward.SenderComment = dto.Comment;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(MapToDTO(forward));
        }

        [HttpPost("{id}/response")]
        public async Task<ActionResult<TransactionForwardDTO>> Respond(int transactionId, int id, TransactionForwardUpdateDTO dto)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null)
                return NotFound();

            forward.Status = dto.Status;
            forward.ReceiverComment = dto.Comment;
            forward.ReceiverSeen = true;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(MapToDTO(forward));
        }

        [HttpPatch("{id}/response")]
        public async Task<ActionResult<TransactionForwardDTO>> UpdateResponse(int transactionId, int id, TransactionForwardUpdateDTO dto)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null)
                return NotFound();

            forward.Status = dto.Status;
            forward.ReceiverComment = dto.Comment;
            forward.ReceiverSeen = true;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(MapToDTO(forward));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<TransactionForwardDTO>> Remove(int transactionId, int id)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null)
                return NotFound();

            _context.TransactionForwards.Remove(forward);
            await _context.SaveChangesAsync();

            return Ok(MapToDTO(forward));
        }

        private TransactionForwardDTO MapToDTO(TransactionForward f)
        {
            return new TransactionForwardDTO
            {
                Id = f.Id,
                Status = f.Status,
                SenderComment = f.SenderComment,
                ReceiverComment = f.ReceiverComment,
                SenderSeen = f.SenderSeen,
                ReceiverSeen = f.ReceiverSeen,
                ForwardedAt = f.ForwardedAt,
                UpdatedAt = f.UpdatedAt,
                TransactionId = f.TransactionId,
                Sender = new UserResponseDTO(f.Sender),
                Receiver = new UserResponseDTO(f.Receiver)
            };
        }
    }
}
