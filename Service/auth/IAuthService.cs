using EcomerceBE.Models;
using System.Security.Claims;

namespace EcomerceBE.Service.auth
{
    public interface IAuthService
    {
        string GenerateJwtToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        string HashPassword(string password);
        bool VerifyPassword(string inputPassword, string storedHash);
    }

}
