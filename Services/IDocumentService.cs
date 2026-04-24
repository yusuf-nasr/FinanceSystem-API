using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using Microsoft.AspNetCore.Http;

namespace FinanceSystem_Dotnet.Services
{
    public interface IDocumentService
    {
        Task<(DocumentResponseDTO Document, int Id)?> CreateDocumentAsync(IFormFile file, int uploaderId);
        Task<string?> ValidateFile(IFormFile file);
        Task<PaginatedResult<DocumentResponseDTO>> GetDocumentsByUploaderPaginatedAsync(int uploaderId, int page, int perPage);
        Task<DocumentResponseDTO?> GetDocumentByIdAsync(int id);
        Task<(byte[] Content, string Title)?> DownloadDocumentAsync(int id);
        Task<DocumentResponseDTO> DeleteDocumentAsync(int id, int userId, Role role);
        Task<bool> IsVisibleToUser(int documentId, int userId);
    }
}
