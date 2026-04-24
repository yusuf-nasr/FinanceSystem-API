using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
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

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        private Role GetCurrentUserRole()
        {
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<Role>(roleStr, out var role) ? role : Role.USER;
        }

        // POST /api/v1/documents — Upload a document
        [HttpPost]
        public async Task<ActionResult<DocumentResponseDTO>> Create(IFormFile file)
        {
            var validationError = await _documentService.ValidateFile(file);
            if (validationError != null)
                return BadRequest(validationError);

            var uploaderId = GetCurrentUserId();
            var result = await _documentService.CreateDocumentAsync(file, uploaderId);

            if (result == null) return BadRequest();

            return CreatedAtAction(nameof(FindOne), new { id = result.Value.Id }, result.Value.Document);
        }

        // GET /api/v1/documents/uploaded — Documents uploaded by the current user
        [HttpGet("uploaded")]
        public async Task<ActionResult> FindAll([FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            var uploaderId = GetCurrentUserId();
            var paginated = await _documentService.GetDocumentsByUploaderPaginatedAsync(uploaderId, page, perPage);
            return Ok(paginated);
        }

        // GET /api/v1/documents/:id
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentResponseDTO>> FindOne(int id)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                throw new ApiException(404, ErrorCode.DOCUMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });

            // Non-admins can only view documents they are part of
            if (role != Role.ADMIN && !await _documentService.IsVisibleToUser(id, userId))
                throw new ApiException(403, ErrorCode.NOT_DOCUMENT_VIEWER,
                    new Dictionary<string, object> { { "documentId", id } });

            return Ok(document);
        }

        // GET /api/v1/documents/:id/download
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            // Check document exists first
            var meta = await _documentService.GetDocumentByIdAsync(id);
            if (meta == null)
                throw new ApiException(404, ErrorCode.DOCUMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });

            // Non-admins can only download documents they are part of
            if (role != Role.ADMIN && !await _documentService.IsVisibleToUser(id, userId))
                throw new ApiException(403, ErrorCode.NOT_DOCUMENT_VIEWER,
                    new Dictionary<string, object> { { "documentId", id } });

            var result = await _documentService.DownloadDocumentAsync(id);
            if (result == null)
                throw new ApiException(404, ErrorCode.DOCUMENT_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });

            return File(result.Value.Content, "application/pdf", result.Value.Title);
        }

        // DELETE /api/v1/documents/:id
        [HttpDelete("{id}")]
        public async Task<ActionResult<DocumentResponseDTO>> Remove(int id)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var result = await _documentService.DeleteDocumentAsync(id, userId, role);
            return Ok(result);
        }
    }
}
