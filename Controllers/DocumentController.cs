using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly FinanceDbContext _context;

        public DocumentController(FinanceDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<DocumentResponseDTO>> Create(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            if (file.ContentType != "application/pdf")
                return BadRequest("Only PDF files are allowed");

            if (file.Length > 1024 * 1024 * 5)
                return BadRequest("File size cannot exceed 5MB");

            var uploaderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

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

            return CreatedAtAction(nameof(FindOne), new { id = document.Id }, MapToResponse(document));
        }

        [HttpGet("uploaded")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<DocumentResponseDTO>>> FindAll()
        {
            var uploaderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var documents = await _context.Documents
                .Where(d => d.UploaderId == uploaderId)
                .ToListAsync();

            return Ok(documents.Select(MapToResponse));
        }

        [HttpGet("{id}")]
        [Authorize]

        public async Task<ActionResult<DocumentResponseDTO>> FindOne(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound();

            return Ok(MapToResponse(document));
        }

        [HttpGet("{id}/download")]
        [Authorize]

        public async Task<IActionResult> Download(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound();

            return File(document.Content, "application/pdf", document.Title);
        }

        [HttpDelete("{id}")]
        [Authorize]

        public async Task<IActionResult> Remove(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound();

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return Ok(MapToResponse(document));
        }

        private DocumentResponseDTO MapToResponse(Document document)
        {
            return new DocumentResponseDTO
            {
                Id = document.Id,
                Title = document.Title,
                URI = $"/api/v1/Document/{document.Id}/download",
                UploadedAt = document.UploadedAt,
                UploaderId = document.UploaderId
            };
        }
    }
}
