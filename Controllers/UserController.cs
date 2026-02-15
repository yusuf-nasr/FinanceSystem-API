using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
//int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly FinanceDbContext context;

        public UserController(FinanceDbContext _context)
        {
            context = _context;
        }
        [Authorize]
        [HttpPost("create")]
        public async Task<ActionResult> CreateUser([FromBody] UserCreateDTO request)
        {
            if (context.Users.Any(u => u.Name == request.Name))
            {
                return BadRequest(new { message = "Username already exists" });
            }
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            context.Users.Add(new Models.User
            {
                Name = request.Name,
                HashedPassword = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                Active = true,
                Role = request.role,
                DepartmentName = request.DepartmentName
            });
            await context.SaveChangesAsync();
            return Ok(new { message = $"User {request.Name} created successfully" });
        }

    }
}