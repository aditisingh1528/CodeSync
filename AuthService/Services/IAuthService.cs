using AuthService.DTOs;
using AuthService.Models;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<IEnumerable<UserSummaryDto>> GetAllUsersAsync();
        Task<string> GetServiceStatusAsync();

        // register and login
        Task<(bool Success, string Message, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginDto dto);

        // admin can promote/demote users
        Task<(bool Success, string Message)> UpdateUserRoleAsync(UpdateRoleDto dto);
    }
}
