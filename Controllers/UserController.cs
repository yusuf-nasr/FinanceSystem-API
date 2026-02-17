using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly IFinanceService services;

        public UserController(FinanceDbContext _context, IFinanceService services)
        {
            context = _context;
            this.services = services;
        }
        [Authorize]
        [HttpPost("create")]
        public async Task<ActionResult> CreateUser([FromBody] UserCreateDTO request)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!services.IsAdmin(UID))
            {
                return Forbid();
            }
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

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetUsers()
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!services.IsAdmin(UID))
            {
                return Forbid();
            }
            var users = await context.Users.Select(u => new UserResponseDTO(u)).ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> GetUserById(int id)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (context.Users.Find(UID).Role != Role.ADMIN || UID != id)
            {
                return Forbid();
            }
            var user = await context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(new UserResponseDTO(user));
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UserUpdateDTO request)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!services.IsAdmin(UID) || UID != id)
            {
                return Forbid();
            }
            var user = await context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            if (!string.IsNullOrEmpty(request.Name))
            {
                user.Name = request.Name;
            }
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            if (request.Active.ToString() != "")
            {
                user.Active = request.Active;
            }
            if (request.role.ToString() != "")
            {
                user.Role = request.role;
            }
            if (!string.IsNullOrEmpty(request.DepartmentName))
            {
                user.DepartmentName = request.DepartmentName;
            }
            await context.SaveChangesAsync();
            return Ok(new { message = $"User {user.Name} updated successfully" });
        }
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteUser(int id)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!services.IsAdmin(UID))
            {
                return Forbid();
            }
            var user = await context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            context.Users.Remove(user);
            await context.SaveChangesAsync();
            return Ok(new { message = $"User {user.Name} deleted successfully" });
        }

    }
}