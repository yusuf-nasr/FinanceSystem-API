using FinanceSystem_Dotnet.DTOs;

namespace FinanceSystem_Dotnet.Services
{
    public interface ITransactionForwardService
    {
        Task<TransactionForwardDTO?> CreateAsync(int transactionId, TransactionForwardCreateDTO dto, int senderId);
        Task<List<TransactionForwardDTO>> FindAllAsync(int transactionId);
        Task<PaginatedResult<TransactionForwardDTO>> FindAllPaginatedAsync(int transactionId, int page, int perPage);
        Task<TransactionForwardDTO?> FindOneAsync(int transactionId, int id);
        Task<TransactionForwardDTO?> UpdateSenderAsync(int transactionId, int id, TransactionForwardSenderUpdateDTO dto, int senderId);
        Task<TransactionForwardDTO?> RespondAsync(int transactionId, int id, TransactionForwardUpdateDTO dto, int receiverId);
        Task<TransactionForwardDTO?> UpdateResponseAsync(int transactionId, int id, TransactionForwardUpdateDTO dto, int receiverId);
        Task<TransactionForwardDTO?> DeleteAsync(int transactionId, int id, int senderId);
        Task MarkAsSeenAsync(int transactionId, int forwardId, int userId);
    }
}
