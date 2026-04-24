using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceSystem_Dotnet.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly FinanceDbContext _context;

        public TransactionService(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<TransactionDTO> CreateAsync(TransactionCreateDTO dto, int creatorId)
        {
            var transaction = new Transaction
            {
                Title = dto.Title,
                Description = dto.Description,
                TransactionTypeName = dto.TransactionTypeName,
                Priority = dto.Priority,
                CreatorId = creatorId,
                CreatedAt = DateTime.UtcNow,
                Fulfilled = false
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            if (dto.DocumentIds != null && dto.DocumentIds.Any())
            {
                foreach (var docId in dto.DocumentIds)
                {
                    _context.TransactionDocuments.Add(new TransactionDocument
                    {
                        TransactionId = transaction.Id,
                        DocumentId = docId,
                        AttachedBy = creatorId,
                        AttachedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }

            return MapToDTO(transaction);
        }

        public async Task<TransactionListResultDTO> FindAllAsync(TransactionQuery? query, int userId, bool isAdmin, int page, int perPage)
        {
            IQueryable<Transaction> transactionsQuery = _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards);

            if (query == TransactionQuery.All)
            {
                if (!isAdmin)
                    throw new ApiException(403, ErrorCode.MISSING_ROLE);
            }
            else if (query == TransactionQuery.Inbox)
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    (t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault() != null
                        && t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault().ReceiverId == userId
                        && !t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault().SenderSeen) ||
                    (!t.Forwards.Any() && t.CreatorId == userId)
                );
            }
            else if (query == TransactionQuery.Outgoing)
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault() != null
                    && t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault().SenderId == userId
                    && t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault().SenderSeen
                );
            }
            else // Archive or default
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    (t.CreatorId == userId && (!t.Forwards.Any() || t.Forwards.Any(f => f.SenderId != userId && f.ReceiverId != userId))) ||
                    t.Forwards.Any(f => f.SenderId == userId || f.ReceiverId == userId)
                );
            }

            transactionsQuery = transactionsQuery.OrderByDescending(t => t.CreatedAt);

            var totalCount = await transactionsQuery.CountAsync();
            var lastPage = (int)Math.Ceiling((double)totalCount / perPage);
            var items = await transactionsQuery.Skip((page - 1) * perPage).Take(perPage).ToListAsync();

            var allForStatus = await transactionsQuery.Select(t => new
            {
                LastForwardStatus = t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault() != null
                    ? (TransactionForwardStatus?)t.Forwards.OrderByDescending(f => f.Id).First().Status
                    : null
            }).ToListAsync();

            var summary = new Dictionary<string, int>
            {
                { "WAITING", allForStatus.Count(x => x.LastForwardStatus == TransactionForwardStatus.WAITING) },
                { "APPROVED", allForStatus.Count(x => x.LastForwardStatus == TransactionForwardStatus.APPROVED) },
                { "REJECTED", allForStatus.Count(x => x.LastForwardStatus == TransactionForwardStatus.REJECTED) },
                { "NEEDS_EDITING", allForStatus.Count(x => x.LastForwardStatus == TransactionForwardStatus.NEEDS_EDITING) },
                { "NO_FORWARD", allForStatus.Count(x => x.LastForwardStatus == null) }
            };

            return new TransactionListResultDTO
            {
                Data = items.Select(MapToListItemDTO).ToList(),
                Pagination = new PaginationMeta
                {
                    Total = totalCount,
                    LastPage = lastPage,
                    CurrentPage = page,
                    PerPage = perPage,
                    Prev = page > 1 ? page - 1 : null,
                    Next = page < lastPage ? page + 1 : null
                },
                Summary = summary
            };
        }

        public async Task<TransactionDTO?> FindOneAsync(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return null;
            return MapToDTO(transaction);
        }

        public async Task<bool> IsParticipant(int transactionId, int userId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return false;
            if (transaction.CreatorId == userId) return true;

            if (transaction.Forwards != null &&
                transaction.Forwards.Any(f => f.SenderId == userId || f.ReceiverId == userId))
                return true;

            return false;
        }

        public async Task<bool> IsCreator(int transactionId, int userId)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);
            return transaction?.CreatorId == userId;
        }

        public async Task<TransactionForward?> FindLatestForward(int transactionId)
        {
            return await _context.TransactionForwards
                .Where(f => f.TransactionId == transactionId)
                .OrderByDescending(f => f.Id)
                .FirstOrDefaultAsync();
        }

        public async Task MarkAsSeenAsync(int transactionId, int userId)
        {
            // Node.js uses updateMany to mark ALL forwards for sender/receiver as seen
            var forwardsAsSender = await _context.TransactionForwards
                .Where(f => f.TransactionId == transactionId && f.SenderId == userId && !f.SenderSeen)
                .ToListAsync();
            foreach (var f in forwardsAsSender)
                f.SenderSeen = true;

            var forwardsAsReceiver = await _context.TransactionForwards
                .Where(f => f.TransactionId == transactionId && f.ReceiverId == userId && !f.ReceiverSeen)
                .ToListAsync();
            foreach (var f in forwardsAsReceiver)
                f.ReceiverSeen = true;

            if (forwardsAsSender.Any() || forwardsAsReceiver.Any())
                await _context.SaveChangesAsync();
        }

        public async Task ResetSenderSeenAsync(int transactionId, int receiverUserId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return;

            var latestForward = transaction.Forwards?.OrderByDescending(f => f.Id).FirstOrDefault();
            if (latestForward == null) return;

            if (latestForward.ReceiverId == receiverUserId && latestForward.SenderSeen)
            {
                latestForward.SenderSeen = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsAttacher(int transactionId, int documentId, int userId)
        {
            var td = await _context.TransactionDocuments
                .FirstOrDefaultAsync(td => td.TransactionId == transactionId && td.DocumentId == documentId);
            return td != null && td.AttachedBy == userId;
        }

        public async Task CheckRestriction(int transactionId, int userId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new ApiException(404, ErrorCode.TRANSACTION_NOT_FOUND);

            var latestForward = transaction.Forwards?.OrderByDescending(f => f.Id).FirstOrDefault();
            if (latestForward == null) return;

            if (latestForward.Status != TransactionForwardStatus.WAITING)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_RESPONDED);

            if (latestForward.ReceiverSeen)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_SEEN);
        }

        public async Task CheckIfFulfilled(int id)
        {
            var transaction = await _context.Transactions
                .Select(t => new { t.Id, t.Fulfilled })
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction?.Fulfilled == true)
                throw new ApiException(403, ErrorCode.TRANSACTION_ALREADY_FULFILLED);
        }

        public async Task<TransactionDTO?> UpdateAsync(int id, TransactionUpdateDTO dto, int userId, Role role)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Documents)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return null;

            // Check if setting fulfilled=true requires budget info
            if (dto.Fulfilled == true)
            {
                if (string.IsNullOrEmpty(dto.BudgetName) || dto.BudgetAllocation == null)
                    throw new ApiException(400, ErrorCode.MISSING_BUDGET_INFO);
            }

            // Guard: cannot mutate a fulfilled transaction (unless explicitly un-fulfilling it)
            // Node.js: if (updateTransactionDto.fulfilled !== false) await this.checkIfFulfilled(id);
            if (dto.Fulfilled != false)
                await CheckIfFulfilled(id);

            // Apply null-safe partial updates
            if (dto.Title != null) transaction.Title = dto.Title;
            if (dto.Description != null) transaction.Description = dto.Description;
            if (dto.TransactionTypeName != null) transaction.TransactionTypeName = dto.TransactionTypeName;
            if (dto.Priority.HasValue) transaction.Priority = dto.Priority.Value;
            if (dto.Fulfilled.HasValue) transaction.Fulfilled = dto.Fulfilled.Value;
            if (dto.BudgetName != null) transaction.BudgetName = dto.BudgetName;
            if (dto.BudgetAllocation.HasValue) transaction.BudgetAllocation = dto.BudgetAllocation.Value;

            await _context.SaveChangesAsync();

            // Reload to get fresh navigation properties
            transaction = await _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards)
                .FirstAsync(t => t.Id == id);

            return MapToDTO(transaction);
        }

        public async Task<TransactionDTO?> DeleteAsync(int id, Role role)
        {
            await CheckIfFulfilled(id);

            var transaction = await _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return null;

            try
            {
                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // FK violation: transaction still has forwards — cannot delete
                throw new ApiException(409, ErrorCode.TRANSACTION_HAS_FORWARDS,
                    new Dictionary<string, object> { { "id", id } });
            }

            return MapToDTO(transaction);
        }

        public async Task<TransactionDTO?> AttachDocumentAsync(int transactionId, int documentId, int userId)
        {
            await CheckIfFulfilled(transactionId);

            var existing = await _context.TransactionDocuments
                .FirstOrDefaultAsync(td => td.TransactionId == transactionId && td.DocumentId == documentId);

            if (existing == null)
            {
                _context.TransactionDocuments.Add(new TransactionDocument
                {
                    TransactionId = transactionId,
                    DocumentId = documentId,
                    AttachedBy = userId,
                    AttachedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            await ResetSenderSeenAsync(transactionId, userId);

            return await FindOneAsync(transactionId);
        }

        public async Task<TransactionDTO?> DetachDocumentAsync(int transactionId, int documentId, int userId, Role role)
        {
            await CheckIfFulfilled(transactionId);

            var existing = await _context.TransactionDocuments
                .FirstOrDefaultAsync(td => td.TransactionId == transactionId && td.DocumentId == documentId);

            if (existing == null)
                return await FindOneAsync(transactionId);

            // Only the person who attached can detach (admin bypasses this check)
            if (role != Role.ADMIN && existing.AttachedBy != userId)
                throw new ApiException(403, ErrorCode.NOT_DOCUMENT_ATTACHER,
                    new Dictionary<string, object> { { "transactionId", transactionId }, { "documentId", documentId } });

            _context.TransactionDocuments.Remove(existing);
            await _context.SaveChangesAsync();

            await ResetSenderSeenAsync(transactionId, userId);

            return await FindOneAsync(transactionId);
        }

        private TransactionDTO MapToDTO(Transaction t)
        {
            return new TransactionDTO
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Fulfilled = t.Fulfilled,
                Priority = t.Priority,
                CreatedAt = t.CreatedAt,
                CreatorId = t.CreatorId,
                TransactionTypeName = t.TransactionTypeName,
                BudgetName = t.BudgetName,
                BudgetAllocation = t.BudgetAllocation,
                LastForwardStatus = t.Forwards?.OrderByDescending(f => f.Id).FirstOrDefault()?.Status,
                Documents = t.Documents?.Select(d => new DocumentResponseDTO
                {
                    Id = d.Id,
                    Title = d.Title,
                    URI = $"/api/v1/documents/{d.Id}/download",
                    UploadedAt = d.UploadedAt,
                    UploaderId = d.UploaderId
                }).ToList()
            };
        }

        private TransactionListItemDTO MapToListItemDTO(Transaction t)
        {
            return new TransactionListItemDTO
            {
                Id = t.Id,
                Title = t.Title,
                Fulfilled = t.Fulfilled,
                Priority = t.Priority,
                TransactionTypeName = t.TransactionTypeName,
                LastForwardStatus = t.Forwards?.OrderByDescending(f => f.Id).FirstOrDefault()?.Status,
                DocumentsCount = t.Documents?.Count ?? 0
            };
        }
    }
}
