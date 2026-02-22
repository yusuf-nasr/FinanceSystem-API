using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
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
            // Validate forward creation
            await ValidateForwardCreation(transactionId, senderId);

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

        public async Task<PaginatedResult<TransactionForwardDTO>> FindAllPaginatedAsync(int transactionId, int page, int perPage)
        {
            var forwards = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Where(f => f.TransactionId == transactionId)
                .OrderByDescending(f => f.ForwardedAt)
                .ToListAsync();

            var dtos = forwards.Select(MapToDTO).ToList();
            return PaginatedResult<TransactionForwardDTO>.Create(dtos, page, perPage);
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

        public async Task MarkAsSeenAsync(int transactionId, int forwardId, int userId)
        {
            var forward = await _context.TransactionForwards
                .FirstOrDefaultAsync(f => f.Id == forwardId && f.TransactionId == transactionId);

            if (forward == null) return;

            if (forward.SenderId == userId && !forward.SenderSeen)
            {
                forward.SenderSeen = true;
                forward.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else if (forward.ReceiverId == userId && !forward.ReceiverSeen)
            {
                forward.ReceiverSeen = true;
                forward.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<TransactionForwardDTO?> UpdateSenderAsync(int transactionId, int id, TransactionForwardSenderUpdateDTO dto, int senderId)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            // Validate sender identity
            if (forward.SenderId != senderId)
                throw new ApiException(403, ErrorCode.NOT_FORWARD_SENDER);

            // Can only update if forward hasn't been responded to
            if (forward.Status != TransactionForwardStatus.WAITING)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_RESPONDED);

            forward.SenderComment = dto.Comment;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(forward);
        }

        public async Task<TransactionForwardDTO?> RespondAsync(int transactionId, int id, TransactionForwardUpdateDTO dto, int receiverId)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            // Validate receiver identity
            if (forward.ReceiverId != receiverId)
                throw new ApiException(403, ErrorCode.NOT_FORWARD_RECEIVER);

            // Sender must not have seen (otherwise it's too late to respond fresh)
            if (forward.SenderSeen && forward.Status != TransactionForwardStatus.WAITING)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_RESPONDED);

            // Must be the latest forward
            await ValidateIsLatestForward(transactionId, id);

            forward.Status = dto.Status;
            forward.ReceiverComment = dto.Comment;
            forward.ReceiverSeen = true;
            forward.SenderSeen = false;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(forward);
        }

        public async Task<TransactionForwardDTO?> UpdateResponseAsync(int transactionId, int id, TransactionForwardUpdateDTO dto, int receiverId)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            // Validate receiver identity
            if (forward.ReceiverId != receiverId)
                throw new ApiException(403, ErrorCode.NOT_FORWARD_RECEIVER);

            // Forward must have been responded to already
            if (forward.Status == TransactionForwardStatus.WAITING)
                throw new ApiException(403, ErrorCode.FORWARD_NOT_RESPONDED);

            // Sender must not have seen the response yet
            if (forward.SenderSeen)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_SEEN);

            // Must be the latest forward
            await ValidateIsLatestForward(transactionId, id);

            forward.Status = dto.Status;
            forward.ReceiverComment = dto.Comment;
            forward.ReceiverSeen = true;
            forward.SenderSeen = false;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(forward);
        }

        public async Task<TransactionForwardDTO?> DeleteAsync(int transactionId, int id, int senderId)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            // Only sender can undo (delete) a forward
            if (forward.SenderId != senderId)
                throw new ApiException(403, ErrorCode.NOT_FORWARD_SENDER);

            // Can't undo if receiver has already seen it
            if (forward.ReceiverSeen)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_SEEN);

            _context.TransactionForwards.Remove(forward);
            await _context.SaveChangesAsync();

            return MapToDTO(forward);
        }

        public async Task<TransactionForwardDTO?> EditSenderCommentAsync(int transactionId, int id, string? comment, int senderId)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            if (forward.SenderId != senderId)
                throw new ApiException(403, ErrorCode.NOT_FORWARD_SENDER);

            await ValidateIsLatestForward(transactionId, id);

            forward.SenderComment = comment;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(forward);
        }

        public async Task<TransactionForwardDTO?> EditReceiverCommentAsync(int transactionId, int id, string? comment, int receiverId)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            if (forward.ReceiverId != receiverId)
                throw new ApiException(403, ErrorCode.NOT_FORWARD_RECEIVER);

            await ValidateIsLatestForward(transactionId, id);

            forward.ReceiverComment = comment;
            forward.SenderSeen = false;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(forward);
        }

        private async Task ValidateForwardCreation(int transactionId, int senderId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new ApiException(404, ErrorCode.TRANSACTION_NOT_FOUND);

            var forwards = transaction.Forwards?.OrderByDescending(f => f.Id).ToList() ?? new List<TransactionForward>();

            if (!forwards.Any())
            {
                // No forwards exist — only the creator can create the first forward
                if (transaction.CreatorId != senderId)
                    throw new ApiException(403, ErrorCode.NOT_TRANSACTION_CREATOR);
            }
            else
            {
                var latestForward = forwards.First();

                // Only the latest forward's receiver can re-forward
                if (latestForward.ReceiverId != senderId)
                    throw new ApiException(403, ErrorCode.NOT_LATEST_RECEIVER);

                // The latest forward must have been responded to before re-forwarding
                if (latestForward.Status == TransactionForwardStatus.WAITING)
                    throw new ApiException(403, ErrorCode.FORWARD_NOT_RESPONDED);
            }
        }

        private async Task ValidateIsLatestForward(int transactionId, int forwardId)
        {
            var latestForward = await _context.TransactionForwards
                .Where(f => f.TransactionId == transactionId)
                .OrderByDescending(f => f.Id)
                .FirstOrDefaultAsync();

            if (latestForward == null || latestForward.Id != forwardId)
                throw new ApiException(403, ErrorCode.NOT_LATEST_RECEIVER);
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
