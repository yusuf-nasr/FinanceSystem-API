using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceSystem_Dotnet.Services
{
    public class TransactionTypeService : ITransactionTypeService
    {
        private readonly FinanceDbContext _context;

        public TransactionTypeService(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> CreateAsync(TransactionTypeCreateDTO request, int creatorId)
        {
            if (_context.TransactionTypes.FirstOrDefault(t => t.Name == request.Name) != null)
            {
                return (false, "Transaction type already exists.");
            }

            var transactionType = new TransactionType
            {
                Name = request.Name,
                CreatorId = creatorId
            };

            _context.TransactionTypes.Add(transactionType);
            await _context.SaveChangesAsync();
            return (true, "Transaction type created successfully.");
        }

        public async Task<List<TransactionTypeResponseDTO>> GetAllAsync()
        {
            return await _context.TransactionTypes.Select(t => new TransactionTypeResponseDTO
            {
                CreatorId = t.CreatorId,
                Name = t.Name
            }).ToListAsync();
        }

        public async Task<PaginatedResult<TransactionTypeResponseDTO>> GetAllPaginatedAsync(int page, int perPage)
        {
            var query = _context.TransactionTypes.Select(t => new TransactionTypeResponseDTO
            {
                CreatorId = t.CreatorId,
                Name = t.Name
            });

            return await PaginatedResult<TransactionTypeResponseDTO>.CreateAsync(query, page, perPage);
        }

        public async Task<TransactionTypeResponseDTO?> GetByNameAsync(string name)
        {
            var transactionType = await _context.TransactionTypes.FirstOrDefaultAsync(t => t.Name == name);
            if (transactionType == null) return null;

            return new TransactionTypeResponseDTO
            {
                CreatorId = transactionType.CreatorId,
                Name = transactionType.Name
            };
        }

        public async Task<(bool Success, string Message)> DeleteAsync(string name)
        {
            var transactionType = await _context.TransactionTypes.FirstOrDefaultAsync(t => t.Name == name);
            if (transactionType == null)
            {
                return (false, "Transaction type not found.");
            }

            _context.TransactionTypes.Remove(transactionType);
            await _context.SaveChangesAsync();
            return (true, "Transaction type deleted successfully.");
        }
    }
}
