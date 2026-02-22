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

            // Get total count and paginated results
            var totalCount = await transactionsQuery.CountAsync();
            var lastPage = (int)Math.Ceiling((double)totalCount / perPage);
            var items = await transactionsQuery.Skip((page - 1) * perPage).Take(perPage).ToListAsync();

            // Compute summary counts from the full result set
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

            var latestForward = transaction.Forwards?.OrderByDescending(f => f.Id).FirstOrDefault();
            if (latestForward != null && (latestForward.SenderId == userId || latestForward.ReceiverId == userId))
                return true;

            return false;
        }

        public async Task<bool> IsLastReceiver(int transactionId, int userId)
        {
            var latestForward = await _context.TransactionForwards
                .Where(f => f.TransactionId == transactionId)
                .OrderByDescending(f => f.Id)
                .FirstOrDefaultAsync();

            return latestForward != null && latestForward.ReceiverId == userId;
        }

        public async Task MarkAsSeenAsync(int transactionId, int userId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return;

            var latestForward = transaction.Forwards?.OrderByDescending(f => f.Id).FirstOrDefault();
            if (latestForward == null) return;

            if (latestForward.SenderId == userId && !latestForward.SenderSeen)
            {
                latestForward.SenderSeen = true;
                await _context.SaveChangesAsync();
            }
            else if (latestForward.ReceiverId == userId && !latestForward.ReceiverSeen)
            {
                latestForward.ReceiverSeen = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ResetSenderSeenAsync(int transactionId, int receiverUserId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return;

            var latestForward = transaction.Forwards?.OrderByDescending(f => f.Id).FirstOrDefault();
            if (latestForward == null) return;

            // Only reset if the current user is the receiver of the latest forward
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
            if (latestForward == null) return; // No forwards, no restriction

            // If the latest forward has been seen or responded to, restrict document changes
            if (latestForward.Status != TransactionForwardStatus.WAITING)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_RESPONDED);

            if (latestForward.ReceiverSeen)
                throw new ApiException(403, ErrorCode.FORWARD_ALREADY_SEEN);
        }

        public async Task<TransactionDTO?> UpdateAsync(int id, TransactionUpdateDTO dto, int userId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Documents)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return null;

            if (dto.Title != null) transaction.Title = dto.Title;
            if (dto.Description != null) transaction.Description = dto.Description;
            if (dto.TransactionTypeName != null) transaction.TransactionTypeName = dto.TransactionTypeName;
            transaction.Priority = dto.Priority;
            transaction.Fulfilled = dto.Fulfilled;

            if (dto.DocumentIds != null)
            {
                var existingDocs = await _context.TransactionDocuments.Where(td => td.TransactionId == id).ToListAsync();
                _context.TransactionDocuments.RemoveRange(existingDocs);

                foreach (var docId in dto.DocumentIds)
                {
                    _context.TransactionDocuments.Add(new TransactionDocument
                    {
                        TransactionId = id,
                        DocumentId = docId,
                        AttachedBy = userId,
                        AttachedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Reload to get fresh navigation properties
            transaction = await _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards)
                .FirstAsync(t => t.Id == id);

            return MapToDTO(transaction);
        }

        public async Task<TransactionDTO?> DeleteAsync(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return null;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return MapToDTO(transaction);
        }

        public async Task<TransactionDTO?> AttachDocumentAsync(int transactionId, int documentId, int userId)
        {
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

            // Reset sender seen since receiver modified the transaction
            await ResetSenderSeenAsync(transactionId, userId);

            return await FindOneAsync(transactionId);
        }

        public async Task<TransactionDTO?> DetachDocumentAsync(int transactionId, int documentId, int userId)
        {
            var existing = await _context.TransactionDocuments
                .FirstOrDefaultAsync(td => td.TransactionId == transactionId && td.DocumentId == documentId);

            if (existing == null)
                return await FindOneAsync(transactionId);

            // Only the person who attached can detach
            if (existing.AttachedBy != userId)
                throw new ApiException(403, ErrorCode.NOT_DOCUMENT_ATTACHER);

            // Document must have been attached during the latest forward
            var latestForward = await _context.TransactionForwards
                .Where(f => f.TransactionId == transactionId)
                .OrderByDescending(f => f.Id)
                .FirstOrDefaultAsync();

            if (latestForward == null || existing.AttachedAt < latestForward.ForwardedAt)
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_PARTICIPANT);

            // User must be sender or receiver of the latest forward
            if (latestForward.SenderId != userId && latestForward.ReceiverId != userId)
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_PARTICIPANT);

            _context.TransactionDocuments.Remove(existing);
            await _context.SaveChangesAsync();

            // Reset sender seen since the transaction was modified
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
