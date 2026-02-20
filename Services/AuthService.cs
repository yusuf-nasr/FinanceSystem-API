using FinanceSystem_Dotnet.DAL;
using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinanceSystem_Dotnet.Services
{
    public class AuthService : IAuthService
    {
        private readonly FinanceDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(FinanceDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public (UserResponseDTO User, string AccessToken, string RefreshToken)? Login(string name, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Name == name);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.HashedPassword))
            {
                return null;
            }

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(user);
            user.LastLogin = DateTime.UtcNow;
            _context.SaveChanges();

            return (new UserResponseDTO(user), accessToken, refreshToken);
        }

        public (UserResponseDTO User, string AccessToken, string RefreshToken)? Refresh(string refreshToken)
        {
            var principal = ValidateToken(refreshToken);
            if (principal == null) return null;

            var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tokenType = principal.FindFirst("token_type")?.Value;

            if (userIdStr == null || tokenType != "refresh") return null;

            var userId = int.Parse(userIdStr);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return null;

            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken(user);

            return (new UserResponseDTO(user), newAccessToken, newRefreshToken);
        }

        private string GenerateAccessToken(User user)
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

        private string GenerateRefreshToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("token_type", "refresh"),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!))
                }, out _);

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
