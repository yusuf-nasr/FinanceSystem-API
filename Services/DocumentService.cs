using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
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

        public async Task<List<DocumentResponseDTO>> GetDocumentsByUploaderAsync(int uploaderId)
        {
            var documents = await _context.Documents
                .Where(d => d.UploaderId == uploaderId)
                .ToListAsync();

            return documents.Select(MapToResponse).ToList();
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

        public async Task<DocumentResponseDTO?> DeleteDocumentAsync(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null) return null;

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return MapToResponse(document);
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
