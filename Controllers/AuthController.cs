using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
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
                throw new ApiException(401, ErrorCode.INVALID_CREDENTIALS);

            return Ok(new
            {
                access_token = result.Value.AccessToken,
                refresh_token = result.Value.RefreshToken,
                user = result.Value.User,
            });
        }

        [HttpPost("refresh")]
        public ActionResult Refresh([FromBody] RefreshTokenDto request)
        {
            var result = _authService.Refresh(request.RefreshToken);
            if (result == null)
                throw new ApiException(401, ErrorCode.INVALID_REFRESH_TOKEN);

            return Ok(new
            {
                access_token = result.Value.AccessToken,
                refresh_token = result.Value.RefreshToken,
                user = result.Value.User,
            });
        }
    }

    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; }
    }
}
