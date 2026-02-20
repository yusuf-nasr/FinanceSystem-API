using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
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

        public async Task<List<TransactionDTO>> FindAllAsync(TransactionQuery? query, int userId, bool isAdmin)
        {
            IQueryable<Transaction> transactionsQuery = _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards);

            if (query == TransactionQuery.All)
            {
                if (!isAdmin)
                    return null; // Signal forbidden
            }
            else if (query == TransactionQuery.Inbox)
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    (t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault() != null && t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault().ReceiverId == userId) ||
                    (!t.Forwards.Any() && t.CreatorId == userId)
                );
            }
            else if (query == TransactionQuery.Outgoing)
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault() != null && t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault().SenderId == userId
                );
            }
            else // Archive or default
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    (t.CreatorId == userId && (!t.Forwards.Any() || t.Forwards.Any(f => f.SenderId != userId && f.ReceiverId != userId))) ||
                    t.Forwards.Any(f => f.SenderId == userId || f.ReceiverId == userId)
                );
            }

            var list = await transactionsQuery.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return list.Select(MapToDTO).ToList();
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

            return await FindOneAsync(transactionId);
        }

        public async Task<TransactionDTO?> DetachDocumentAsync(int transactionId, int documentId)
        {
            var existing = await _context.TransactionDocuments
                .FirstOrDefaultAsync(td => td.TransactionId == transactionId && td.DocumentId == documentId);

            if (existing != null)
            {
                _context.TransactionDocuments.Remove(existing);
                await _context.SaveChangesAsync();
            }

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
                    URI = $"/api/v1/Document/{d.Id}/download",
                    UploadedAt = d.UploadedAt,
                    UploaderId = d.UploaderId
                }).ToList()
            };
        }
    }
}
