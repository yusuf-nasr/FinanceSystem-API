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
            // Validate receiver exists
            var receiverExists = await _context.Users.AnyAsync(u => u.Id == dto.ReceiverId);
            if (!receiverExists)
                throw new ApiException(404, ErrorCode.TRANSACTION_FORWARD_RECEIVER_NOT_FOUND,
                    new Dictionary<string, object> { { "receiverId", dto.ReceiverId.ToString() } });

            // Validate forward creation (includes fulfilled check)
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
                SenderSeenAt = DateTime.UtcNow,
                ReceiverSeen = false,
                ReceiverSeenAt = null
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
                forward.SenderSeenAt = DateTime.UtcNow;
                forward.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else if (forward.ReceiverId == userId && !forward.ReceiverSeen)
            {
                forward.ReceiverSeen = true;
                forward.ReceiverSeenAt = DateTime.UtcNow;
                forward.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<TransactionForwardDTO?> UpdateSenderAsync(int transactionId, int id, TransactionForwardSenderUpdateDTO? dto, int senderId)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Include(f => f.Transaction)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            // Guard: fulfilled transaction cannot be mutated
            if (forward.Transaction.Fulfilled)
                throw new ApiException(403, ErrorCode.TRANSACTION_ALREADY_FULFILLED, new Dictionary<string, object> { { "transactionId", transactionId.ToString() } });

            // Validate sender identity
            if (forward.SenderId != senderId)
                throw new ApiException(403, ErrorCode.NOT_FORWARD_SENDER, new Dictionary<string, object> { { "forwardId", id.ToString() } });

            // Can only update if forward hasn't been responded to
            if (forward.Status != TransactionForwardStatus.WAITING)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_RESPONDED, new Dictionary<string, object> { { "forwardId", id.ToString() } });

            if (dto != null)
            {
                forward.SenderComment = dto.Comment;
            }
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(forward);
        }

        public Task<TransactionForwardDTO?> RespondAsync(int transactionId, int id, TransactionForwardUpdateDTO dto, int receiverId)
        {
            return UpdateResponseAsync(transactionId, id, dto, receiverId);
        }

        public async Task<TransactionForwardDTO?> UpdateResponseAsync(int transactionId, int id, TransactionForwardUpdateDTO dto, int receiverId)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Include(f => f.Transaction)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            // Guard: fulfilled transaction cannot be mutated
            if (forward.Transaction.Fulfilled)
                throw new ApiException(403, ErrorCode.TRANSACTION_ALREADY_FULFILLED, new Dictionary<string, object> { { "transactionId", transactionId.ToString() } });

            // Validate receiver identity
            if (forward.ReceiverId != receiverId)
                throw new ApiException(403, ErrorCode.NOT_FORWARD_RECEIVER, new Dictionary<string, object> { { "forwardId", id.ToString() } });

            // Sender must not have seen the response yet
            if (forward.SenderSeen && forward.ReceiverComment != null)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_SEEN, new Dictionary<string, object> { { "forwardId", id.ToString() } });

            // Must be the latest forward
            await ValidateIsLatestForward(transactionId, id);

            forward.Status = dto.Status;
            forward.ReceiverComment = dto.Comment;
            forward.ReceiverSeen = true;
            forward.ReceiverSeenAt = DateTime.UtcNow;
            forward.SenderSeen = false;
            forward.SenderSeenAt = null;
            forward.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(forward);
        }

        public async Task<TransactionForwardDTO?> DeleteAsync(int transactionId, int id, int senderId)
        {
            var forward = await _context.TransactionForwards
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Include(f => f.Transaction)
                .FirstOrDefaultAsync(f => f.Id == id && f.TransactionId == transactionId);

            if (forward == null) return null;

            // Guard: fulfilled transaction cannot be mutated
            if (forward.Transaction.Fulfilled)
                throw new ApiException(403, ErrorCode.TRANSACTION_ALREADY_FULFILLED, new Dictionary<string, object> { { "transactionId", transactionId.ToString() } });

            // Only sender can undo (delete) a forward
            if (forward.SenderId != senderId)
                throw new ApiException(403, ErrorCode.NOT_FORWARD_SENDER, new Dictionary<string, object> { { "forwardId", id.ToString() } });

            // Can't undo if receiver has already seen it
            if (forward.ReceiverSeen)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_SEEN, new Dictionary<string, object> { { "forwardId", id.ToString() } });

            _context.TransactionForwards.Remove(forward);
            await _context.SaveChangesAsync();

            return MapToDTO(forward);
        }

        private async Task ValidateForwardCreation(int transactionId, int senderId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new ApiException(404, ErrorCode.TRANSACTION_NOT_FOUND, new Dictionary<string, object> { { "transactionId", transactionId.ToString() } });

            // Guard: fulfilled transaction cannot be mutated
            if (transaction.Fulfilled)
                throw new ApiException(403, ErrorCode.TRANSACTION_ALREADY_FULFILLED, new Dictionary<string, object> { { "transactionId", transactionId.ToString() } });

            var forwards = transaction.Forwards?.OrderByDescending(f => f.Id).ToList() ?? new List<TransactionForward>();

            if (!forwards.Any())
            {
                // No forwards exist — only the creator can create the first forward
                if (transaction.CreatorId != senderId)
                    throw new ApiException(403, ErrorCode.NOT_TRANSACTION_CREATOR, new Dictionary<string, object> { { "transactionId", transactionId.ToString() } });
            }
            else
            {
                var latestForward = forwards.First();

                // Only the latest forward's receiver can re-forward
                if (latestForward.ReceiverId != senderId)
                    throw new ApiException(403, ErrorCode.NOT_LATEST_RECEIVER, new Dictionary<string, object> { { "transactionId", transactionId.ToString() } });

                // The latest forward must have been responded to before re-forwarding
                if (latestForward.Status == TransactionForwardStatus.WAITING)
                    throw new ApiException(403, ErrorCode.FORWARD_NOT_RESPONDED, new Dictionary<string, object> { { "transactionId", transactionId.ToString() } });
            }
        }

        private async Task ValidateIsLatestForward(int transactionId, int forwardId)
        {
            var latestForward = await _context.TransactionForwards
                .Where(f => f.TransactionId == transactionId)
                .OrderByDescending(f => f.Id)
                .FirstOrDefaultAsync();

            if (latestForward == null || latestForward.Id != forwardId)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_RESPONDED,
                    new Dictionary<string, object> { { "forwardId", forwardId.ToString() } });
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
                SenderSeenAt = f.SenderSeenAt,
                ReceiverSeen = f.ReceiverSeen,
                ReceiverSeenAt = f.ReceiverSeenAt,
                ForwardedAt = f.ForwardedAt,
                UpdatedAt = f.UpdatedAt,
                TransactionId = f.TransactionId,
                Sender = new UserResponseDTO(f.Sender),
                Receiver = new UserResponseDTO(f.Receiver)
            };
        }
    }
}
