using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public ActionResult Login([FromBody] UserLoginDTO request)
        {
            var result = _authService.Login(request.Name, request.Password);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            return Ok(new
            {
                message = "Login successful",
                user = result.Value.User,
                access_token = result.Value.AccessToken,
                refresh_token = result.Value.RefreshToken
            });
        }

        [HttpPost("refresh")]
        public ActionResult Refresh([FromBody] RefreshTokenDto request)
        {
            var result = _authService.Refresh(request.RefreshToken);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            return Ok(new
            {
                user = result.Value.User,
                access_token = result.Value.AccessToken,
                refresh_token = result.Value.RefreshToken
            });
        }
    }

    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; }
    }
}
