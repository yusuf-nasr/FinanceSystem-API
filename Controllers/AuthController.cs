using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly FinanceDbContext context;
        private readonly IConfiguration _config;

        public AuthController(FinanceDbContext _context, IConfiguration config)
        {
            context = _context;
            _config = config;
        }

        [HttpPost("login")]
        public ActionResult Login([FromBody] UserLoginDTO request)
        {
            var user = context.Users.FirstOrDefault(u => u.Name == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.HashedPassword))
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }
            var Jtoken = GenerateToken(user);
            return Ok(new { message = "Login successful",
                user = new UserResponseDTO(user),
                token = Jtoken
            });
        }
        private string GenerateToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
