using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceSystem_Dotnet.Services
{
    public interface IBudgetCategoryService
    {
        Task<BudgetCategoryDTO> CreateAsync(string name);
        Task<PaginatedResult<BudgetCategoryDTO>> FindAllAsync(BudgetCategoryQueryDTO query);
        Task<BudgetCategoryDTO> FindOneAsync(string name);
        Task<BudgetCategoryDTO> UpdateAsync(string name, UpdateBudgetCategoryDTO dto);
        Task<BudgetCategoryDTO> DeleteAsync(string name);
        Task<PaginatedResult<BudgetEntryDTO>> FindAllEntriesAsync(string budgetName, BudgetEntryQueryDTO query);
        Task<BudgetEntryDTO> AddEntryAsync(string budgetName, CreateBudgetEntryDTO dto, int userId);
        Task<BudgetEntryDTO> RemoveEntryAsync(string budgetName, int id);
    }

    public class BudgetCategoryService : IBudgetCategoryService
    {
        private readonly FinanceDbContext _context;

        public BudgetCategoryService(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<BudgetCategoryDTO> CreateAsync(string name)
        {
            var existing = await _context.BudgetCategories.FindAsync(name);
            if (existing != null)
                throw new ApiException(409, ErrorCode.BUDGET_CATEGORY_ALREADY_EXISTS,
                    new Dictionary<string, object> { { "name", name } });

            var category = new BudgetCategory { Name = name };
            _context.BudgetCategories.Add(category);
            await _context.SaveChangesAsync();

            return new BudgetCategoryDTO { Name = name, Budget = 0, Allocated = 0, Available = 0 };
        }

        public async Task<PaginatedResult<BudgetCategoryDTO>> FindAllAsync(BudgetCategoryQueryDTO query)
        {
            var q = _context.BudgetCategories
                .Include(bc => bc.Entries)
                .Include(bc => bc.Transactions)
                .AsQueryable();

            if (!string.IsNullOrEmpty(query.Name))
                q = q.Where(bc => bc.Name.ToLower().Contains(query.Name.ToLower()));

            var total = await q.CountAsync();
            var lastPage = (int)Math.Ceiling((double)total / query.PerPage);
            var categories = await q
                .Skip((query.Page - 1) * query.PerPage)
                .Take(query.PerPage)
                .ToListAsync();

            return new PaginatedResult<BudgetCategoryDTO>
            {
                Data = categories.Select(MapToDTO).ToList(),
                Pagination = new PaginationMeta
                {
                    Total = total,
                    LastPage = lastPage,
                    CurrentPage = query.Page,
                    PerPage = query.PerPage,
                    Prev = query.Page > 1 ? query.Page - 1 : null,
                    Next = query.Page < lastPage ? query.Page + 1 : null
                }
            };
        }

        public async Task<BudgetCategoryDTO> FindOneAsync(string name)
        {
            var category = await _context.BudgetCategories
                .Include(bc => bc.Entries)
                .Include(bc => bc.Transactions)
                .FirstOrDefaultAsync(bc => bc.Name == name);

            if (category == null)
                throw new ApiException(404, ErrorCode.BUDGET_CATEGORY_NOT_FOUND,
                    new Dictionary<string, object> { { "name", name } });

            return MapToDTO(category);
        }

        public async Task<BudgetCategoryDTO> UpdateAsync(string name, UpdateBudgetCategoryDTO dto)
        {
            var category = await _context.BudgetCategories
                .Include(bc => bc.Entries)
                .Include(bc => bc.Transactions)
                .FirstOrDefaultAsync(bc => bc.Name == name);

            if (category == null)
                throw new ApiException(404, ErrorCode.BUDGET_CATEGORY_NOT_FOUND,
                    new Dictionary<string, object> { { "name", name } });

            // Check new name doesn't conflict
            if (await _context.BudgetCategories.AnyAsync(bc => bc.Name == dto.NewName))
                throw new ApiException(409, ErrorCode.BUDGET_CATEGORY_ALREADY_EXISTS,
                    new Dictionary<string, object> { { "name", dto.NewName } });

            // Rename: create new → migrate references → delete old (PK rename workaround)
            var newCategory = new BudgetCategory { Name = dto.NewName };
            _context.BudgetCategories.Add(newCategory);
            await _context.SaveChangesAsync();

            var entries = await _context.BudgetEntries.Where(e => e.BudgetName == name).ToListAsync();
            foreach (var entry in entries) entry.BudgetName = dto.NewName;

            var transactions = await _context.Transactions.Where(t => t.BudgetName == name).ToListAsync();
            foreach (var tx in transactions) tx.BudgetName = dto.NewName;

            await _context.SaveChangesAsync();

            _context.BudgetCategories.Remove(category);
            await _context.SaveChangesAsync();

            var updated = await _context.BudgetCategories
                .Include(bc => bc.Entries)
                .Include(bc => bc.Transactions)
                .FirstAsync(bc => bc.Name == dto.NewName);

            return MapToDTO(updated);
        }

        public async Task<BudgetCategoryDTO> DeleteAsync(string name)
        {
            var category = await _context.BudgetCategories
                .Include(bc => bc.Entries)
                .Include(bc => bc.Transactions)
                .FirstOrDefaultAsync(bc => bc.Name == name);

            if (category == null)
                throw new ApiException(404, ErrorCode.BUDGET_CATEGORY_NOT_FOUND,
                    new Dictionary<string, object> { { "name", name } });

            var dto = MapToDTO(category);

            try
            {
                _context.BudgetCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ApiException(409, ErrorCode.BUDGET_CATEGORY_IN_USE,
                    new Dictionary<string, object> { { "name", name } });
            }

            return dto;
        }

        public async Task<PaginatedResult<BudgetEntryDTO>> FindAllEntriesAsync(string budgetName, BudgetEntryQueryDTO query)
        {
            var categoryExists = await _context.BudgetCategories.AnyAsync(bc => bc.Name == budgetName);
            if (!categoryExists)
                throw new ApiException(404, ErrorCode.BUDGET_CATEGORY_NOT_FOUND,
                    new Dictionary<string, object> { { "name", budgetName } });

            var q = _context.BudgetEntries
                .Include(be => be.Inputter)
                .Where(be => be.BudgetName == budgetName)
                .AsQueryable();

            if (!string.IsNullOrEmpty(query.Inputter))
                q = q.Where(be => be.Inputter.Name.ToLower().Contains(query.Inputter.ToLower()));

            if (query.MinAmount.HasValue)
                q = q.Where(be => be.Amount >= query.MinAmount.Value);

            if (query.MaxAmount.HasValue)
                q = q.Where(be => be.Amount <= query.MaxAmount.Value);

            if (query.From.HasValue)
                q = q.Where(be => be.CreatedAt >= query.From.Value);

            if (query.To.HasValue)
                q = q.Where(be => be.CreatedAt <= query.To.Value);

            var total = await q.CountAsync();
            var lastPage = (int)Math.Ceiling((double)total / query.PerPage);
            var entries = await q
                .OrderBy(be => be.Id)
                .Skip((query.Page - 1) * query.PerPage)
                .Take(query.PerPage)
                .ToListAsync();

            return new PaginatedResult<BudgetEntryDTO>
            {
                Data = entries.Select(MapEntryToDTO).ToList(),
                Pagination = new PaginationMeta
                {
                    Total = total,
                    LastPage = lastPage,
                    CurrentPage = query.Page,
                    PerPage = query.PerPage,
                    Prev = query.Page > 1 ? query.Page - 1 : null,
                    Next = query.Page < lastPage ? query.Page + 1 : null
                }
            };
        }

        public async Task<BudgetEntryDTO> AddEntryAsync(string budgetName, CreateBudgetEntryDTO dto, int userId)
        {
            var categoryExists = await _context.BudgetCategories.AnyAsync(bc => bc.Name == budgetName);
            if (!categoryExists)
                throw new ApiException(404, ErrorCode.BUDGET_CATEGORY_NOT_FOUND,
                    new Dictionary<string, object> { { "budgetName", budgetName } });

            var entry = new BudgetEntry
            {
                BudgetName = budgetName,
                Amount = dto.Amount,
                InputterId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.BudgetEntries.Add(entry);
            await _context.SaveChangesAsync();

            return MapEntryToDTO(entry);
        }

        public async Task<BudgetEntryDTO> RemoveEntryAsync(string budgetName, int id)
        {
            // Only the latest entry (highest id) in the category can be removed
            var lastEntry = await _context.BudgetEntries
                .Where(be => be.BudgetName == budgetName)
                .OrderByDescending(be => be.Id)
                .Select(be => new { be.Id })
                .FirstOrDefaultAsync();

            if (lastEntry == null || lastEntry.Id != id)
                throw new ApiException(403, ErrorCode.NOT_LATEST_BUDGET_ENTRY,
                    new Dictionary<string, object> { { "id", id } });

            var entry = await _context.BudgetEntries.FindAsync(id);
            if (entry == null)
                throw new ApiException(404, ErrorCode.BUDGET_ENTRY_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });

            var dto = MapEntryToDTO(entry);
            _context.BudgetEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return dto;
        }

        private BudgetCategoryDTO MapToDTO(BudgetCategory category)
        {
            var budget = category.Entries?.Sum(e => e.Amount) ?? 0;
            var allocated = category.Transactions?
                .Where(t => t.Fulfilled && t.BudgetAllocation.HasValue)
                .Sum(t => t.BudgetAllocation!.Value) ?? 0;

            return new BudgetCategoryDTO
            {
                Name = category.Name,
                Budget = budget,
                Allocated = allocated,
                Available = budget - allocated
            };
        }

        private BudgetEntryDTO MapEntryToDTO(BudgetEntry entry)
        {
            return new BudgetEntryDTO
            {
                Id = entry.Id,
                InputterId = entry.InputterId,
                Amount = entry.Amount,
                BudgetName = entry.BudgetName,
                CreatedAt = entry.CreatedAt
            };
        }
    }
}
