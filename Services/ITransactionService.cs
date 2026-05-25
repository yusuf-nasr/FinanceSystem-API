using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Models;

namespace FinanceSystem_Dotnet.Services
{
    public interface ITransactionService
    {
        Task<TransactionDTO> CreateAsync(TransactionCreateDTO dto, int creatorId);
        Task<TransactionListResultDTO> FindAllAsync(TransactionFilterDTO filter, int userId, bool isAdmin, Role role);
        Task<TransactionDTO?> FindOneAsync(int id);
        Task<TransactionDTO?> UpdateAsync(int id, TransactionUpdateDTO dto, int userId, Role role);
        Task<TransactionDTO?> DeleteAsync(int id, Role role);
        Task<TransactionDTO?> AttachDocumentAsync(int transactionId, int documentId, int userId);
        Task<TransactionDTO?> DetachDocumentAsync(int transactionId, int documentId, int userId, Role role);
        Task<bool> IsParticipant(int transactionId, int userId);
        Task<bool> IsCreator(int transactionId, int userId);
        Task MarkAsSeenAsync(int transactionId, int userId);
        Task<bool> IsAttacher(int transactionId, int documentId, int userId);
        Task CheckIfFulfilled(int id);
        Task<TransactionForward?> FindLatestForward(int transactionId);
    }
}
