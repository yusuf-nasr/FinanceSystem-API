using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.Services
{
    public interface ITransactionService
    {
        Task<TransactionDTO> CreateAsync(TransactionCreateDTO dto, int creatorId);
        Task<TransactionListResultDTO> FindAllAsync(TransactionQuery? query, int userId, bool isAdmin, int page, int perPage);
        Task<TransactionDTO?> FindOneAsync(int id);
        Task<TransactionDTO?> UpdateAsync(int id, TransactionUpdateDTO dto, int userId);
        Task<TransactionDTO?> DeleteAsync(int id);
        Task<TransactionDTO?> AttachDocumentAsync(int transactionId, int documentId, int userId);
        Task<TransactionDTO?> DetachDocumentAsync(int transactionId, int documentId, int userId);
        Task<bool> IsParticipant(int transactionId, int userId);
        Task MarkAsSeenAsync(int transactionId, int userId);
        Task<bool> IsAttacher(int transactionId, int documentId, int userId);
        Task CheckRestriction(int transactionId, int userId);
    }
}
