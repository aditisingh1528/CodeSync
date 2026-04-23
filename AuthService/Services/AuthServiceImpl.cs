using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;

namespace AuthService.Services
{
    public class AuthServiceImpl : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthServiceImpl(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public Task<string> GetServiceStatusAsync()
        {
            return Task.FromResult("AuthService is running!");
        }

        // UC-2: Register a new user
        public async Task<(bool Success, string Message, AuthResponseDto? Data)> RegisterAsync(RegisterDto dto)
        {
            // Check if email is already taken
            if (await _userRepository.EmailExistsAsync(dto.Email))
                return (false, "Email is already registered.", null);

            // Check if username is already taken
            if (await _userRepository.UsernameExistsAsync(dto.Username))
                return (false, "Username is already taken.", null);

            // Hash the password before saving - never store plain text passwords!
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                Email    = dto.Email.ToLower(),
                PasswordHash = passwordHash,
                CreatedAt    = DateTime.UtcNow
            };

            await _userRepository.AddUserAsync(user);

            var response = new AuthResponseDto
            {
                Id        = user.Id,
                Username  = user.Username,
                Email     = user.Email,
                CreatedAt = user.CreatedAt,
                Message   = "Registration successful!"
            };

            return (true, "Registration successful!", response);
        }

        // UC-2: Login with email + password
        public async Task<(bool Success, string Message, AuthResponseDto? Data)> LoginAsync(LoginDto dto)
        {
            // Look up the user by email
            var user = await _userRepository.GetUserByEmailAsync(dto.Email);

            // Use a generic error message - don't reveal whether it's email or password that's wrong
            if (user == null)
                return (false, "Invalid email or password.", null);

            // Verify the password against the stored hash
            bool passwordMatches = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!passwordMatches)
                return (false, "Invalid email or password.", null);

            var response = new AuthResponseDto
            {
                Id        = user.Id,
                Username  = user.Username,
                Email     = user.Email,
                CreatedAt = user.CreatedAt,
                Message   = "Login successful!"
            };

            return (true, "Login successful!", response);
        }
    }
}
