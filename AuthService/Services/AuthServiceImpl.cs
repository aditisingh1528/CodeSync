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
    }
}
