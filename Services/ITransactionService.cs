using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.Services
{
    public interface ITransactionService
    {
        Task<TransactionDTO> CreateAsync(TransactionCreateDTO dto, int creatorId);
        Task<List<TransactionDTO>> FindAllAsync(TransactionQuery? query, int userId, bool isAdmin);
        Task<TransactionDTO?> FindOneAsync(int id);
        Task<TransactionDTO?> UpdateAsync(int id, TransactionUpdateDTO dto, int userId);
        Task<TransactionDTO?> DeleteAsync(int id);
        Task<TransactionDTO?> AttachDocumentAsync(int transactionId, int documentId, int userId);
        Task<TransactionDTO?> DetachDocumentAsync(int transactionId, int documentId);
    }
}
