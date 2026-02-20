using FinanceSystem_Dotnet.DTOs;

namespace FinanceSystem_Dotnet.Services
{
    public interface ITransactionTypeService
    {
        Task<(bool Success, string Message)> CreateAsync(TransactionTypeCreateDTO request, int creatorId);
        Task<List<TransactionTypeResponseDTO>> GetAllAsync();
        Task<TransactionTypeResponseDTO?> GetByNameAsync(string name);
        Task<(bool Success, string Message)> DeleteAsync(string name);
    }
}
