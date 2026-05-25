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
        private readonly INotificationService _notificationService;

        public TransactionService(FinanceDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<TransactionDTO> CreateAsync(TransactionCreateDTO dto, int creatorId)
        {
            // Validate transaction type exists
            var typeExists = await _context.TransactionTypes.AnyAsync(t => t.Name == dto.TransactionTypeName);
            if (!typeExists)
                throw new ApiException(404, ErrorCode.TRANSACTION_TYPE_NOT_FOUND,
                    new Dictionary<string, object> { { "typeName", dto.TransactionTypeName } });

            // Validate all documents exist
            if (dto.DocumentIds != null && dto.DocumentIds.Any())
            {
                foreach (var docId in dto.DocumentIds)
                {
                    var docExists = await _context.Documents.AnyAsync(d => d.Id == docId);
                    if (!docExists)
                        throw new ApiException(404, ErrorCode.DOCUMENT_NOT_FOUND,
                            new Dictionary<string, object> { { "documentId", docId.ToString() } });
                }
            }

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

            // Reload to get fresh navigation properties including Documents and Forwards
            transaction = await _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards)
                .FirstAsync(t => t.Id == transaction.Id);

            return MapToDTO(transaction);
        }

        public async Task<TransactionListResultDTO> FindAllAsync(TransactionFilterDTO filter, int userId, bool isAdmin, Role role)
        {
            IQueryable<Transaction> transactionsQuery = _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards);

            // Apply query type filter (inbox/outgoing/all/archive)
            if (filter.Query == TransactionQuery.All)
            {
                if (!isAdmin)
                    throw new ApiException(403, ErrorCode.MISSING_ROLE);
            }
            else if (filter.Query == TransactionQuery.Inbox)
            {
                // Node: latestForward.receiverId === userId (as receiver)
                // OR no forwards and is creator
                transactionsQuery = transactionsQuery.Where(t =>
                    // Has forwards and user is the latest receiver
                    (t.Forwards.Any() &&
                     t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault()!.ReceiverId == userId) ||
                    // No forwards and user is creator
                    (!t.Forwards.Any() && t.CreatorId == userId)
                );
            }
            else if (filter.Query == TransactionQuery.Outgoing)
            {
                // Node: sender has WAITING forward (latest forward senderId === userId and status is WAITING)
                // OR no forwards and is creator (already in inbox, so this matches Node's outgoing)
                transactionsQuery = transactionsQuery.Where(t =>
                    t.Forwards.Any() &&
                    t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault()!.SenderId == userId &&
                    t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault()!.Status == TransactionForwardStatus.WAITING
                );
            }
            else // Archive or default
            {
                // User is a participant (creator or in any forward) but NOT in inbox or outgoing
                transactionsQuery = transactionsQuery.Where(t =>
                    (t.CreatorId == userId || t.Forwards.Any(f => f.SenderId == userId || f.ReceiverId == userId)) &&
                    // Not inbox
                    !(
                        (t.Forwards.Any() &&
                         t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault()!.ReceiverId == userId) ||
                        (!t.Forwards.Any() && t.CreatorId == userId)
                    ) &&
                    // Not outgoing
                    !(
                        t.Forwards.Any() &&
                        t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault()!.SenderId == userId &&
                        t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault()!.Status == TransactionForwardStatus.WAITING
                    )
                );
            }

            // Apply text/field filters (matching Node's TransactionFilterDto)
            if (!string.IsNullOrEmpty(filter.Title))
                transactionsQuery = transactionsQuery.Where(t => t.Title.ToLower().Contains(filter.Title.ToLower()));

            if (!string.IsNullOrEmpty(filter.Description))
                transactionsQuery = transactionsQuery.Where(t => t.Description.ToLower().Contains(filter.Description.ToLower()));

            if (!string.IsNullOrEmpty(filter.TypeName))
                transactionsQuery = transactionsQuery.Where(t => t.TransactionTypeName.ToLower().Contains(filter.TypeName.ToLower()));

            if (filter.Fulfilled.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.Fulfilled == filter.Fulfilled.Value);

            if (filter.Priority.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.Priority == filter.Priority.Value);

            if (filter.CreatorId.HasValue)
            {
                if (!isAdmin)
                    throw new ApiException(403, ErrorCode.RESTRICTED_FIELD_UPDATE,
                        new Dictionary<string, object> { { "fields", "creatorId" } });
                transactionsQuery = transactionsQuery.Where(t => t.CreatorId == filter.CreatorId.Value);
            }

            if (filter.From.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.CreatedAt >= filter.From.Value);

            if (filter.To.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.CreatedAt <= filter.To.Value);

            // Filter by lastForwardStatus
            if (filter.LastForwardStatus.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    t.Forwards.Any() &&
                    t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault()!.Status == filter.LastForwardStatus.Value
                );
            }

            transactionsQuery = transactionsQuery.OrderByDescending(t => t.CreatedAt);

            var totalCount = await transactionsQuery.CountAsync();
            var lastPage = (int)Math.Ceiling((double)totalCount / filter.PerPage);
            var items = await transactionsQuery.Skip((filter.Page - 1) * filter.PerPage).Take(filter.PerPage).ToListAsync();

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
                    CurrentPage = filter.Page,
                    PerPage = filter.PerPage,
                    Prev = filter.Page > 1 ? filter.Page - 1 : null,
                    Next = filter.Page < lastPage ? filter.Page + 1 : null
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
            {
                f.SenderSeen = true;
                f.SenderSeenAt = DateTime.UtcNow;
            }

            var forwardsAsReceiver = await _context.TransactionForwards
                .Where(f => f.TransactionId == transactionId && f.ReceiverId == userId && !f.ReceiverSeen)
                .ToListAsync();
            foreach (var f in forwardsAsReceiver)
            {
                f.ReceiverSeen = true;
                f.ReceiverSeenAt = DateTime.UtcNow;
            }

            if (forwardsAsSender.Any() || forwardsAsReceiver.Any())
                await _context.SaveChangesAsync();
        }

        public async Task<bool> IsAttacher(int transactionId, int documentId, int userId)
        {
            var td = await _context.TransactionDocuments
                .FirstOrDefaultAsync(td => td.TransactionId == transactionId && td.DocumentId == documentId);
            return td != null && td.AttachedBy == userId;
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
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return null;

            bool isAdmin = role == Role.ADMIN;
            bool isAccountant = role == Role.ACCOUNTANT;

            // Guard: cannot mutate a fulfilled transaction (unless explicitly un-fulfilling it)
            if (dto.Fulfilled != false)
                await CheckIfFulfilled(id);

            // Validate transaction type name if updated
            if (dto.TransactionTypeName != null)
            {
                var typeExists = await _context.TransactionTypes.AnyAsync(t => t.Name == dto.TransactionTypeName);
                if (!typeExists)
                    throw new ApiException(404, ErrorCode.TRANSACTION_TYPE_NOT_FOUND,
                        new Dictionary<string, object> { { "typeName", dto.TransactionTypeName } });
            }

            // Validate budget name if updated
            if (dto.BudgetName != null)
            {
                var budgetCategory = await _context.BudgetCategories.FirstOrDefaultAsync(bc => bc.Name == dto.BudgetName);
                if (budgetCategory == null)
                    throw new ApiException(404, ErrorCode.BUDGET_CATEGORY_NOT_FOUND,
                        new Dictionary<string, object> { { "categoryName", dto.BudgetName } });
            }

            // If setting fulfilled=true, validate accountant/admin conditions
            if (dto.Fulfilled == true)
            {
                if (string.IsNullOrEmpty(dto.BudgetName) && string.IsNullOrEmpty(transaction.BudgetName))
                    throw new ApiException(400, ErrorCode.MISSING_BUDGET_INFO);

                var budgetNameToCheck = dto.BudgetName ?? transaction.BudgetName;
                var budgetAllocationToCheck = dto.BudgetAllocation ?? transaction.BudgetAllocation;

                if (budgetAllocationToCheck == null)
                    throw new ApiException(400, ErrorCode.MISSING_BUDGET_INFO);

                // Node: Only accountant who is the latest forward receiver can fulfill
                if (isAccountant && !isAdmin)
                {
                    var latestForward = transaction.Forwards?.OrderByDescending(f => f.Id).FirstOrDefault();
                    if (latestForward == null || latestForward.ReceiverId != userId)
                        throw new ApiException(403, ErrorCode.NOT_LATEST_ACCOUNTANT,
                            new Dictionary<string, object> { { "transactionId", id.ToString() } });

                    if (latestForward.Status != TransactionForwardStatus.APPROVED)
                        throw new ApiException(403, ErrorCode.TRANSACTION_NOT_APPROVED,
                            new Dictionary<string, object> { { "transactionId", id.ToString() } });
                }

                // Check budget availability and send warnings if insufficient
                var budgetCategory = await _context.BudgetCategories
                    .Include(bc => bc.Entries)
                    .Include(bc => bc.Transactions)
                    .FirstOrDefaultAsync(bc => bc.Name == budgetNameToCheck);

                if (budgetCategory != null)
                {
                    var budget = (budgetCategory.Entries?.Sum(e => e.Amount) ?? 0) + budgetCategory.Preallocation;
                    var allocated = budgetCategory.Transactions?
                        .Where(t => t.Id != id && t.Fulfilled && t.BudgetAllocation.HasValue)
                        .Sum(t => t.BudgetAllocation!.Value) ?? 0;
                    var available = budget - allocated;

                    if (budgetAllocationToCheck > available)
                    {
                        // Send warning notification to all admins (matching Node behavior)
                        var adminUsers = await _context.Users.Where(u => u.Role == Role.ADMIN).ToListAsync();
                        foreach (var admin in adminUsers)
                        {
                            await _notificationService.CreateNotificationAsync(
                                admin.Id,
                                NotificationType.WARNING,
                                "BUDGET_ALLOCATION_OVERFLOW_ATTEMPT",
                                new
                                {
                                    transactionId = id.ToString(),
                                    categoryName = budgetNameToCheck,
                                    availableAmount = available.ToString(),
                                    requestedAmount = budgetAllocationToCheck.Value.ToString(),
                                    attemptedBy = userId.ToString()
                                }
                            );
                        }

                        throw new ApiException(403, ErrorCode.INSUFFICIENT_BUDGET,
                            new Dictionary<string, object>
                            {
                                { "categoryName", budgetNameToCheck },
                                { "availableAmount", available.ToString() },
                                { "requestedAmount", budgetAllocationToCheck.Value.ToString() }
                            });
                    }
                }
            }

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

            if (transaction.Forwards != null && transaction.Forwards.Any())
            {
                throw new ApiException(409, ErrorCode.TRANSACTION_HAS_FORWARDS,
                    new Dictionary<string, object> { { "transactionId", id.ToString() } });
            }

            try
            {
                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // FK violation: transaction still has forwards — cannot delete
                throw new ApiException(409, ErrorCode.TRANSACTION_HAS_FORWARDS,
                    new Dictionary<string, object> { { "transactionId", id.ToString() } });
            }

            return MapToDTO(transaction);
        }

        public async Task<TransactionDTO?> AttachDocumentAsync(int transactionId, int documentId, int userId)
        {
            // Validate transaction existence
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
                throw new ApiException(404, ErrorCode.TRANSACTION_NOT_FOUND,
                    new Dictionary<string, object> { { "transactionId", transactionId.ToString() } });

            // Guard: cannot mutate fulfilled transaction
            if (transaction.Fulfilled)
                throw new ApiException(403, ErrorCode.TRANSACTION_ALREADY_FULFILLED);

            // Validate document existence
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
                throw new ApiException(404, ErrorCode.DOCUMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "documentId", documentId.ToString() } });

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

            return await FindOneAsync(transactionId);
        }

        public async Task<TransactionDTO?> DetachDocumentAsync(int transactionId, int documentId, int userId, Role role)
        {
            // Validate transaction existence
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
                throw new ApiException(404, ErrorCode.TRANSACTION_NOT_FOUND,
                    new Dictionary<string, object> { { "transactionId", transactionId.ToString() } });

            // Guard: cannot mutate fulfilled transaction
            if (transaction.Fulfilled)
                throw new ApiException(403, ErrorCode.TRANSACTION_ALREADY_FULFILLED);

            // Validate document existence
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
                throw new ApiException(404, ErrorCode.DOCUMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "documentId", documentId.ToString() } });

            var existing = await _context.TransactionDocuments
                .FirstOrDefaultAsync(td => td.TransactionId == transactionId && td.DocumentId == documentId);

            if (existing == null)
                return await FindOneAsync(transactionId);

            // Only the person who attached can detach (admin bypasses this check)
            if (role != Role.ADMIN && existing.AttachedBy != userId)
                throw new ApiException(403, ErrorCode.NOT_DOCUMENT_ATTACHER,
                    new Dictionary<string, object> { { "transactionId", transactionId.ToString() }, { "documentId", documentId.ToString() } });

            _context.TransactionDocuments.Remove(existing);
            await _context.SaveChangesAsync();

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
                    DownloadURI = $"/documents/{d.Id}/download",
                    UploadedAt = d.UploadedAt,
                    UploaderId = d.UploaderId
                }).ToList() ?? new List<DocumentResponseDTO>()
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
                DocumentsCount = t.Documents?.Count ?? 0,
                CreatedAt = t.CreatedAt
            };
        }
    }
}
