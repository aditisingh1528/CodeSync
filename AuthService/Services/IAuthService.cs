using AuthService.Models;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<string> GetServiceStatusAsync();
    }
}
