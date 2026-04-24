using Microsoft.EntityFrameworkCore;

namespace FinanceSystem_Dotnet.DTOs
{
    public class PaginationMeta
    {
        public int Total { get; set; }
        public int LastPage { get; set; }
        public int CurrentPage { get; set; }
        public int PerPage { get; set; }
        public int? Prev { get; set; }
        public int? Next { get; set; }
    }

    public class PaginatedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public PaginationMeta Pagination { get; set; } = new();

        public static PaginatedResult<T> Create(IEnumerable<T> source, int page, int perPage)
        {
            var totalCount = source.Count();
            var lastPage = (int)Math.Ceiling((double)totalCount / perPage);
            var items = source.Skip((page - 1) * perPage).Take(perPage).ToList();

            return new PaginatedResult<T>
            {
                Data = items,
                Pagination = new PaginationMeta
                {
                    Total = totalCount,
                    LastPage = lastPage,
                    CurrentPage = page,
                    PerPage = perPage,
                    Prev = page > 1 ? page - 1 : null,
                    Next = page < lastPage ? page + 1 : null
                }
            };
        }

        public static async Task<PaginatedResult<T>> CreateAsync(IQueryable<T> query, int page, int perPage)
        {
            var totalCount = await query.CountAsync();
            var lastPage = (int)Math.Ceiling((double)totalCount / perPage);
            var items = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync();

            return new PaginatedResult<T>
            {
                Data = items,
                Pagination = new PaginationMeta
                {
                    Total = totalCount,
                    LastPage = lastPage,
                    CurrentPage = page,
                    PerPage = perPage,
                    Prev = page > 1 ? page - 1 : null,
                    Next = page < lastPage ? page + 1 : null
                }
            };
        }
    }
}
