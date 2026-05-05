using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;

namespace AuthService.Services
{
    public class AuthServiceImpl : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService     _jwtService;

        public AuthServiceImpl(IUserRepository userRepository, IJwtService jwtService)
        {
            _userRepository = userRepository;
            _jwtService     = jwtService;
        }

        // Returns UserSummaryDto
        public async Task<IEnumerable<UserSummaryDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return users.Select(u => new UserSummaryDto
            {
                Id        = u.Id,
                Username  = u.Username,
                Email     = u.Email,
                Role      = u.Role,
                CreatedAt = u.CreatedAt
            });
        }

        public Task<string> GetServiceStatusAsync()
            => Task.FromResult("AuthService is running!");

        // hash password, assign default role, issue JWT
        public async Task<(bool Success, string Message, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto)
        {
            if (await _userRepository.EmailExistsAsync(dto.Email))
                return (false, "Email is already registered.", null);

            if (await _userRepository.UsernameExistsAsync(dto.Username))
                return (false, "Username is already taken.", null);

            var user = new User
            {
                Username     = dto.Username,
                Email        = dto.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role         = "User",
                CreatedAt    = DateTime.UtcNow
            };

            await _userRepository.AddUserAsync(user);

            var token = _jwtService.GenerateToken(user);

            return (true, "Registration successful!", new AuthResponseDto
            {
                Id        = user.Id,
                Username  = user.Username,
                Email     = user.Email,
                Role      = user.Role,
                CreatedAt = user.CreatedAt,
                Token     = token,
                Message   = "Registration successful!"
            });
        }

        //verify password, issue fresh JWT
        public async Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.GetUserByEmailAsync(dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return (false, "Invalid email or password.", null);

            var token = _jwtService.GenerateToken(user);

            return (true, "Login successful!", new AuthResponseDto
            {
                Id        = user.Id,
                Username  = user.Username,
                Email     = user.Email,
                Role      = user.Role,
                CreatedAt = user.CreatedAt,
                Token     = token,
                Message   = "Login successful!"
            });
        }

        // update any user's role
        public async Task<(bool Success, string Message)> UpdateUserRoleAsync(UpdateRoleDto dto)
        {
            var updated = await _userRepository.UpdateUserRoleAsync(dto.UserId, dto.Role);

            return updated
                ? (true,  $"User {dto.UserId} role updated to '{dto.Role}' successfully.")
                : (false, $"User with Id {dto.UserId} was not found.");
        }
    }
}
