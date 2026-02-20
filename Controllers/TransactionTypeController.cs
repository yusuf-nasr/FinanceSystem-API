using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Models;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/transactions/types")]
    [ApiController]
    public class TransactionTypeController : ControllerBase
    {
        private readonly FinanceDbContext _context;
        private readonly IFinanceService services;

        public TransactionTypeController(FinanceDbContext context, IFinanceService services)
        {
            _context = context;
            this.services = services;
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTransactionType(TransactionTypeCreateDTO request)
        {
            if (_context.TransactionTypes.FirstOrDefault(t => t.Name == request.Name) != null)
            {
                return BadRequest("Transaction type already exists.");
            }
            var transactionType = new TransactionType
            {
                Name = request.Name,
                CreatorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0")
            };
            _context.TransactionTypes.Add(transactionType);
            await _context.SaveChangesAsync();
            return Ok("Transaction type created successfully.");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetTransactionTypes()
        {
            return Ok(await _context.TransactionTypes.Select(t => new TransactionTypeResponseDTO
            {
                CreatorId = t.CreatorId,
                Name = t.Name
            }).ToListAsync());
        }

        [HttpGet("{name}")]
        [Authorize]
        public async Task<IActionResult> GetTransactionTypeByName(string name)
        {
            var transactionType = await _context.TransactionTypes.FirstOrDefaultAsync(t => t.Name == name);
            if (transactionType == null)
            {
                return NotFound("Transaction type not found.");
            }
            return Ok(new TransactionTypeResponseDTO
            {
                CreatorId = transactionType.CreatorId,
                Name = transactionType.Name
            });
        }

        [HttpDelete("{name}")]
        [Authorize]
        public async Task<IActionResult> DeleteTransactionType(string name)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var transactionType = await _context.TransactionTypes.FirstOrDefaultAsync(t => t.Name == name);
            if (!(services.IsAdmin(UID) || transactionType.CreatorId == UID))
            {
                return Forbid("Only admins or creator can delete transaction types.");
            }
            if (transactionType == null)
            {
                return NotFound("Transaction type not found.");
            }
            _context.TransactionTypes.Remove(transactionType);
            await _context.SaveChangesAsync();
            return Ok("Transaction type deleted successfully.");
        }
    }
}
