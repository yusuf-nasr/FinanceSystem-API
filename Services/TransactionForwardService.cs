using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceSystem_Dotnet.Services
{
    public class TransactionForwardService : ITransactionForwardService
    {
        private readonly FinanceDbContext _context;

        public TransactionForwardService(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<TransactionForwardDTO?> CreateAsync(int transactionId, TransactionForwardCreateDTO dto, int senderId)
        {
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

            return MapToDTO(forward!);
        }

        public async Task<List<TransactionForwardDTO>> FindAllAsync(int transactionId)
        {
            var forwards = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Where(f => f.TransactionId == transactionId)
                .ToListAsync();

            return forwards.Select(MapToDTO).ToList();
        }

        public async Task<TransactionForwardDTO?> FindOneAsync(int transactionId, int id)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;
            return MapToDTO(forward);
        }

        public async Task<TransactionForwardDTO?> UpdateSenderAsync(int transactionId, int id, TransactionForwardSenderUpdateDTO dto)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            forward.SenderComment = dto.Comment;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(forward);
        }

        public async Task<TransactionForwardDTO?> RespondAsync(int transactionId, int id, TransactionForwardUpdateDTO dto)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            forward.Status = dto.Status;
            forward.ReceiverComment = dto.Comment;
            forward.ReceiverSeen = true;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(forward);
        }

        public async Task<TransactionForwardDTO?> UpdateResponseAsync(int transactionId, int id, TransactionForwardUpdateDTO dto)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            forward.Status = dto.Status;
            forward.ReceiverComment = dto.Comment;
            forward.ReceiverSeen = true;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(forward);
        }

        public async Task<TransactionForwardDTO?> DeleteAsync(int transactionId, int id)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            _context.TransactionForwards.Remove(forward);
            await _context.SaveChangesAsync();

            return MapToDTO(forward);
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
