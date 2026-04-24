using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
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

        public async Task<TransactionTypeResponseDTO> CreateAsync(TransactionTypeCreateDTO request, int creatorId)
        {
            if (_context.TransactionTypes.Any(t => t.Name == request.Name))
                throw new ApiException(409, ErrorCode.TRANSACTION_TYPE_ALREADY_EXISTS,
                    new Dictionary<string, object> { { "name", request.Name } });

            var transactionType = new TransactionType
            {
                Name = request.Name,
                CreatorId = creatorId
            };

            _context.TransactionTypes.Add(transactionType);
            await _context.SaveChangesAsync();

            return new TransactionTypeResponseDTO { Name = transactionType.Name, CreatorId = transactionType.CreatorId };
        }

        public async Task<PaginatedResult<TransactionTypeResponseDTO>> FindAllAsync(TransactionTypeQueryDTO query)
        {
            IQueryable<TransactionType> q = _context.TransactionTypes;

            if (query.CreatorId.HasValue)
                q = q.Where(t => t.CreatorId == query.CreatorId.Value);

            var projected = q.Select(t => new TransactionTypeResponseDTO
            {
                CreatorId = t.CreatorId,
                Name = t.Name
            });

            return await PaginatedResult<TransactionTypeResponseDTO>.CreateAsync(projected, query.Page, query.PerPage);
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

        public async Task<TransactionTypeResponseDTO> DeleteAsync(string name, int userId, bool isAdmin)
        {
            var transactionType = await _context.TransactionTypes.FirstOrDefaultAsync(t => t.Name == name);
            if (transactionType == null)
                throw new ApiException(404, ErrorCode.TRANSACTION_TYPE_NOT_FOUND,
                    new Dictionary<string, object> { { "name", name } });

            if (!isAdmin && transactionType.CreatorId != userId)
                throw new ApiException(403, ErrorCode.NOT_TRANSACTION_TYPE_CREATOR);

            try
            {
                _context.TransactionTypes.Remove(transactionType);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ApiException(409, ErrorCode.TRANSACTION_TYPE_IN_USE,
                    new Dictionary<string, object> { { "name", name } });
            }

            return new TransactionTypeResponseDTO { Name = transactionType.Name, CreatorId = transactionType.CreatorId };
        }
    }
}
