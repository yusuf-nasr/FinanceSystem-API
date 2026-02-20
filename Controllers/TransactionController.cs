using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Models;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly FinanceDbContext _context;
        private readonly IFinanceService services;

        public TransactionController(FinanceDbContext context, IFinanceService services)
        {
            _context = context;
            this.services = services;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TransactionDTO>> Create(TransactionCreateDTO dto)
        {
            var creatorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var transaction = new Transaction
            {
                Title = dto.Title,
                Description = dto.Description,
                TransactionTypeName = dto.TransactionTypeName,
                Priority = dto.Priority,
                CreatorId = creatorId,
                CreatedAt = DateTime.UtcNow,
                Fulfilled = false
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            if (dto.DocumentIds != null && dto.DocumentIds.Any())
            {
                foreach (var docId in dto.DocumentIds)
                {
                    _context.TransactionDocuments.Add(new TransactionDocument
                    {
                        TransactionId = transaction.Id,
                        DocumentId = docId,
                        AttachedBy = creatorId,
                        AttachedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(FindOne), new { id = transaction.Id }, await MapToDTO(transaction));
        }

        [HttpGet]
        [Authorize]

        public async Task<ActionResult<IEnumerable<TransactionDTO>>> FindAll([FromQuery] TransactionQuery? query)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            IQueryable<Transaction> transactionsQuery = _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards);

            if (query == TransactionQuery.All)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user?.Role != Role.ADMIN)
                    return Forbid();
            }
            else if (query == TransactionQuery.Inbox)
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    (t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault() != null && t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault().ReceiverId == userId) ||
                    (!t.Forwards.Any() && t.CreatorId == userId)
                );
            }
            else if (query == TransactionQuery.Outgoing)
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault() != null && t.Forwards.OrderByDescending(f => f.Id).FirstOrDefault().SenderId == userId
                );
            }
            else // Archive or default
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    (t.CreatorId == userId && (!t.Forwards.Any() || t.Forwards.Any(f => f.SenderId != userId && f.ReceiverId != userId))) ||
                    t.Forwards.Any(f => f.SenderId == userId || f.ReceiverId == userId)
                );
            }

            var list = await transactionsQuery.OrderByDescending(t => t.CreatedAt).ToListAsync();
            var dtos = list.Select(t => MapToDTOManual(t)).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [Authorize]

        public async Task<ActionResult<TransactionDTO>> FindOne(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
                return NotFound();

            return Ok(MapToDTOManual(transaction));
        }

        [HttpPatch("{id}")]
        [Authorize]

        public async Task<ActionResult<TransactionDTO>> Update(int id, TransactionUpdateDTO dto)
        {
            var creatorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);


            var transaction = await _context.Transactions
                .Include(t => t.Documents)
                .FirstOrDefaultAsync(t => t.Id == id);


            if (!(services.IsAdmin(creatorId) || transaction.Id == creatorId))
            {
                return Forbid("only admins or creator can update transaction");
            }

            if (transaction == null) return NotFound();
            if (dto.Title != null) transaction.Title = dto.Title;
            if (dto.Description != null) transaction.Description = dto.Description;
            if (dto.TransactionTypeName != null) transaction.TransactionTypeName = dto.TransactionTypeName;
            transaction.Priority = dto.Priority;
            transaction.Fulfilled = dto.Fulfilled;

            if (dto.DocumentIds != null)
            {
                var existingDocs = await _context.TransactionDocuments.Where(td => td.TransactionId == id).ToListAsync();
                _context.TransactionDocuments.RemoveRange(existingDocs);

                foreach (var docId in dto.DocumentIds)
                {
                    _context.TransactionDocuments.Add(new TransactionDocument
                    {
                        TransactionId = id,
                        DocumentId = docId,
                        AttachedBy = creatorId,
                        AttachedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Reload to get fresh navigation properties (Documents may have changed)
            transaction = await _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards)
                .FirstAsync(t => t.Id == id);

            return Ok(MapToDTOManual(transaction));
        }

        [HttpDelete("{id}")]
        [Authorize]

        public async Task<ActionResult<TransactionDTO>> Remove(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var transaction = await _context.Transactions
                .Include(t => t.Documents)
                .Include(t => t.Forwards)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (!(services.IsAdmin(userId) || transaction.Id == userId))
            {
                return Forbid("only admins or creator can delete transaction");
            }

            if (transaction == null)
                return NotFound();

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return Ok(MapToDTOManual(transaction));
        }

        [HttpPost("{id}/document/{documentId}")]
        [Authorize]

        public async Task<ActionResult<TransactionDTO>> AttachDocument(int id, int documentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            
            var existing = await _context.TransactionDocuments
                .FirstOrDefaultAsync(td => td.TransactionId == id && td.DocumentId == documentId);

            if (existing == null)
            {
                _context.TransactionDocuments.Add(new TransactionDocument
                {
                    TransactionId = id,
                    DocumentId = documentId,
                    AttachedBy = userId,
                    AttachedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return (await FindOne(id)).Result;
        }

        [HttpDelete("{id}/document/{documentId}")]
        [Authorize]

        public async Task<ActionResult<TransactionDTO>> DetachDocument(int id, int documentId)
        {
            var existing = await _context.TransactionDocuments
                .FirstOrDefaultAsync(td => td.TransactionId == id && td.DocumentId == documentId);

            if (existing != null)
            {
                _context.TransactionDocuments.Remove(existing);
                await _context.SaveChangesAsync();
            }

            return (await FindOne(id)).Result;
        }

        private async Task<TransactionDTO> MapToDTO(Transaction t)
        {
             return MapToDTOManual(t);
        }

        private TransactionDTO MapToDTOManual(Transaction t)
        {
            return new TransactionDTO
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Fulfilled = t.Fulfilled,
                Priority = t.Priority,
                CreatedAt = t.CreatedAt,
                CreatorId = t.CreatorId,
                TransactionTypeName = t.TransactionTypeName,
                LastForwardStatus = t.Forwards?.OrderByDescending(f => f.Id).FirstOrDefault()?.Status,
                Documents = t.Documents?.Select(d => new DocumentResponseDTO
                {
                    Id = d.Id,
                    Title = d.Title,
                    URI = $"/api/v1/Document/{d.Id}/download",
                    UploadedAt = d.UploadedAt,
                    UploaderId = d.UploaderId
                }).ToList()
            };
        }
    }
}
