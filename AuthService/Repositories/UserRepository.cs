using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AuthDbContext _context;

        public UserRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
            => await _context.Users.ToListAsync();

        public async Task<User?> GetUserByIdAsync(int id)
            => await _context.Users.FindAsync(id);

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // UC-2: find user by email (case-insensitive)
        public async Task<User?> GetUserByEmailAsync(string email)
            => await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        // UC-2: check if email is already taken
        public async Task<bool> EmailExistsAsync(string email)
            => await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());

        // UC-2: check if username is already taken
        public async Task<bool> UsernameExistsAsync(string username)
            => await _context.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower());

        // UC-3: update a user's role (Admin only operation)
        public async Task<bool> UpdateUserRoleAsync(int userId, string newRole)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Role = newRole;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
