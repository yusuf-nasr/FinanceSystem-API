using FinanceSystem_Dotnet.DTOs;

namespace FinanceSystem_Dotnet.Services
{
    public interface ITransactionTypeService
    {
        Task<TransactionTypeResponseDTO> CreateAsync(TransactionTypeCreateDTO request, int creatorId);
        Task<PaginatedResult<TransactionTypeResponseDTO>> FindAllAsync(TransactionTypeQueryDTO query);
        Task<TransactionTypeResponseDTO?> GetByNameAsync(string name);
        Task<TransactionTypeResponseDTO> DeleteAsync(string name, int userId, bool isAdmin);
    }
}
