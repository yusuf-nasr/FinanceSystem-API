using FinanceSystem_Dotnet.DTOs;

namespace FinanceSystem_Dotnet.Services
{
    public interface ITransactionForwardService
    {
        Task<TransactionForwardDTO?> CreateAsync(int transactionId, TransactionForwardCreateDTO dto, int senderId);
        Task<List<TransactionForwardDTO>> FindAllAsync(int transactionId);
        Task<TransactionForwardDTO?> FindOneAsync(int transactionId, int id);
        Task<TransactionForwardDTO?> UpdateSenderAsync(int transactionId, int id, TransactionForwardSenderUpdateDTO dto);
        Task<TransactionForwardDTO?> RespondAsync(int transactionId, int id, TransactionForwardUpdateDTO dto);
        Task<TransactionForwardDTO?> UpdateResponseAsync(int transactionId, int id, TransactionForwardUpdateDTO dto);
        Task<TransactionForwardDTO?> DeleteAsync(int transactionId, int id);
    }
}
