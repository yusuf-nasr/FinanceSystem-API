using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FinanceSystem_Dotnet.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly FinanceDbContext _context;

        public DocumentService(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<string?> ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "File is required";
            if (file.ContentType != "application/pdf")
                return "Only PDF files are allowed";
            if (file.Length > 1024 * 1024 * 5)
                return "File size cannot exceed 5MB";
            return null;
        }

        public async Task<(DocumentResponseDTO Document, int Id)?> CreateDocumentAsync(IFormFile file, int uploaderId)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            var document = new Document
            {
                Title = file.FileName,
                Content = memoryStream.ToArray(),
                UploadedAt = DateTime.UtcNow,
                UploaderId = uploaderId
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return (MapToResponse(document), document.Id);
        }

        public async Task<PaginatedResult<DocumentResponseDTO>> GetDocumentsByUploaderPaginatedAsync(int uploaderId, int page, int perPage)
        {
            var query = _context.Documents
                .Where(d => d.UploaderId == uploaderId)
                .Select(d => new DocumentResponseDTO
                {
                    Id = d.Id,
                    Title = d.Title,
                    URI = $"/api/v1/documents/{d.Id}/download",
                    UploadedAt = d.UploadedAt,
                    UploaderId = d.UploaderId
                });

            return await PaginatedResult<DocumentResponseDTO>.CreateAsync(query, page, perPage);
        }

        public async Task<DocumentResponseDTO?> GetDocumentByIdAsync(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null) return null;
            return MapToResponse(document);
        }

        public async Task<(byte[] Content, string Title)?> DownloadDocumentAsync(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null) return null;
            return (document.Content, document.Title);
        }

        public async Task<DocumentResponseDTO> DeleteDocumentAsync(int id, int userId, Role role)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                throw new ApiException(404, ErrorCode.DOCUMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });

            // Only the uploader or admin can delete
            if (role != Role.ADMIN && document.UploaderId != userId)
                throw new ApiException(403, ErrorCode.NOT_DOCUMENT_UPLOADER,
                    new Dictionary<string, object> { { "documentId", id } });

            try
            {
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ApiException(409, ErrorCode.DOCUMENT_ALREADY_USED,
                    new Dictionary<string, object> { { "id", id } });
            }

            return MapToResponse(document);
        }

        /// <summary>
        /// A document is visible to a user if they are the uploader,
        /// or they are a participant in any transaction that references the document.
        /// </summary>
        public async Task<bool> IsVisibleToUser(int documentId, int userId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null) return false;

            // Uploader can always see their own documents
            if (document.UploaderId == userId) return true;

            // Check if the user is a participant in any transaction that contains this document
            var isParticipant = await _context.TransactionDocuments
                .Where(td => td.DocumentId == documentId)
                .AnyAsync(td =>
                    _context.Transactions.Any(t => t.Id == td.TransactionId &&
                        (t.CreatorId == userId ||
                         t.Forwards.Any(f => f.SenderId == userId || f.ReceiverId == userId)))
                );

            return isParticipant;
        }

        private DocumentResponseDTO MapToResponse(Document document)
        {
            return new DocumentResponseDTO
            {
                Id = document.Id,
                Title = document.Title,
                URI = $"/api/v1/documents/{document.Id}/download",
                UploadedAt = document.UploadedAt,
                UploaderId = document.UploaderId
            };
        }
    }
}
