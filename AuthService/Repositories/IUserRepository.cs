using AuthService.Models;

namespace AuthService.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task AddUserAsync(User user);

        // UC-2: needed for register and login
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool>  EmailExistsAsync(string email);
        Task<bool>  UsernameExistsAsync(string username);

        // UC-3: needed for admin role management
        Task<bool> UpdateUserRoleAsync(int userId, string newRole);
    }
}
