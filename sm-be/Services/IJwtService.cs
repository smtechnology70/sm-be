using SM_BE.Models;

namespace SM_BE.Services
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        bool ValidateToken(string token);
        int? GetUserIdFromToken(string token);
    }
}
