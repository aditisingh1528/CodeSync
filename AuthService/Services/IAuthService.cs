using AuthService.DTOs;
using AuthService.Models;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<string> GetServiceStatusAsync();

        // UC-2: Register and Login
        Task<(bool Success, string Message, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginDto dto);
    }
}
