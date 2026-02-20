using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/documents")]
    [ApiController]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<DocumentResponseDTO>> Create(IFormFile file)
        {
            var validationError = await _documentService.ValidateFile(file);
            if (validationError != null)
                return BadRequest(validationError);

            var uploaderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _documentService.CreateDocumentAsync(file, uploaderId);

            if (result == null) return BadRequest();

            return CreatedAtAction(nameof(FindOne), new { id = result.Value.Id }, result.Value.Document);
        }

        [HttpGet("uploaded")]
        [Authorize]
        public async Task<ActionResult> FindAll([FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            var uploaderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var paginated = await _documentService.GetDocumentsByUploaderPaginatedAsync(uploaderId, page, perPage);
            return Ok(paginated);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<DocumentResponseDTO>> FindOne(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();

            return Ok(document);
        }

        [HttpGet("{id}/download")]
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var result = await _documentService.DownloadDocumentAsync(id);
            if (result == null)
                return NotFound();

            return File(result.Value.Content, "application/pdf", result.Value.Title);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Remove(int id)
        {
            var document = await _documentService.DeleteDocumentAsync(id);
            if (document == null)
                return NotFound();

            return Ok(document);
        }
    }
}
