using FinanceSystem_Dotnet.DTOs;

namespace FinanceSystem_Dotnet.Services
{
    public interface IAuthService
    {
        (UserResponseDTO User, string AccessToken, string RefreshToken)? Login(string name, string password);
        (UserResponseDTO User, string AccessToken, string RefreshToken)? Refresh(string refreshToken);
    }
}
