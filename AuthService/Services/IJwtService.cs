using AuthService.Models;

namespace AuthService.Services
{
    // Responsible only for creating JWT tokens
    // Keeping it separate makes it easy to test and swap out later
    public interface IJwtService
    {
        // Takes a User and produces a signed JWT token string
        string GenerateToken(User user);
    }
}
