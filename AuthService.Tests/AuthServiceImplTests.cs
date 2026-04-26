using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using AuthService.Services;
using Moq;
using NUnit.Framework;

namespace AuthService.Tests
{
    // ---------------------------------------------------------------
    // Unit tests for AuthServiceImpl (UC-2 + UC-3)
    //
    // We use Moq to fake the repository and JWT service so these
    // tests never touch a real database or generate real tokens.
    // ---------------------------------------------------------------
    [TestFixture]
    public class AuthServiceImplTests
    {
        private Mock<IUserRepository> _mockRepo    = null!;
        private Mock<IJwtService>     _mockJwt     = null!;
        private AuthServiceImpl       _authService = null!;

        [SetUp]
        public void SetUp()
        {
            _mockRepo    = new Mock<IUserRepository>();
            _mockJwt     = new Mock<IJwtService>();
            _authService = new AuthServiceImpl(_mockRepo.Object, _mockJwt.Object);

            // Default: JWT service always returns a fake token string
            _mockJwt.Setup(j => j.GenerateToken(It.IsAny<User>()))
                    .Returns("fake.jwt.token");
        }

        // =============================================================
        // REGISTER TESTS
        // =============================================================

        [Test]
        public async Task Register_WithNewEmailAndUsername_ShouldSucceed()
        {
            _mockRepo.Setup(r => r.EmailExistsAsync("alice@example.com")).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UsernameExistsAsync("alice")).ReturnsAsync(false);
            _mockRepo.Setup(r => r.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var dto = new RegisterDto { Username = "alice", Email = "alice@example.com", Password = "secret123" };

            var (success, message, data) = await _authService.RegisterAsync(dto);

            Assert.That(success, Is.True);
            Assert.That(data, Is.Not.Null);
            Assert.That(data!.Email, Is.EqualTo("alice@example.com"));
            Assert.That(data.Username, Is.EqualTo("alice"));
        }

        [Test]
        public async Task Register_ShouldReturnJwtToken()
        {
            // Arrange
            _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var dto = new RegisterDto { Username = "alice", Email = "alice@example.com", Password = "pass123" };

            // Act
            var (success, _, data) = await _authService.RegisterAsync(dto);

            // Assert: token should be populated in the response
            Assert.That(success, Is.True);
            Assert.That(data!.Token, Is.Not.Null.And.Not.Empty,
                "Register response must include a JWT token");
        }

        [Test]
        public async Task Register_ShouldAssignUserRoleByDefault()
        {
            User? savedUser = null;
            _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                     .Callback<User>(u => savedUser = u)
                     .Returns(Task.CompletedTask);

            var dto = new RegisterDto { Username = "bob", Email = "bob@example.com", Password = "pass123" };
            await _authService.RegisterAsync(dto);

            Assert.That(savedUser!.Role, Is.EqualTo("User"),
                "Newly registered users should get the 'User' role by default");
        }

        [Test]
        public async Task Register_ShouldCallJwtService_Once()
        {
            _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var dto = new RegisterDto { Username = "carol", Email = "carol@example.com", Password = "pass123" };
            await _authService.RegisterAsync(dto);

            // Verify GenerateToken was called exactly once
            _mockJwt.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Once,
                "JWT token should be generated exactly once per registration");
        }

        [Test]
        public async Task Register_WithDuplicateEmail_ShouldFail_AndNotCallJwt()
        {
            _mockRepo.Setup(r => r.EmailExistsAsync("taken@example.com")).ReturnsAsync(true);

            var dto = new RegisterDto { Username = "bob", Email = "taken@example.com", Password = "pass123" };
            var (success, message, data) = await _authService.RegisterAsync(dto);

            Assert.That(success, Is.False);
            Assert.That(data, Is.Null);

            // JWT should NOT be called if registration fails
            _mockJwt.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never,
                "JWT should not be generated when registration fails");
        }

        [Test]
        public async Task Register_WithDuplicateUsername_ShouldFail()
        {
            _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UsernameExistsAsync("alice")).ReturnsAsync(true);

            var dto = new RegisterDto { Username = "alice", Email = "new@example.com", Password = "pass123" };
            var (success, message, _) = await _authService.RegisterAsync(dto);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("Username"));
        }

        [Test]
        public async Task Register_ShouldNotStorePasswordInPlainText()
        {
            User? savedUser = null;
            _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                     .Callback<User>(u => savedUser = u)
                     .Returns(Task.CompletedTask);

            var dto = new RegisterDto { Username = "carol", Email = "carol@example.com", Password = "mypassword" };
            await _authService.RegisterAsync(dto);

            Assert.That(savedUser!.PasswordHash, Is.Not.EqualTo("mypassword"),
                "Password must be hashed, never stored as plain text");
            Assert.That(savedUser.PasswordHash, Does.StartWith("$2"),
                "BCrypt hashes always start with $2");
        }

        // =============================================================
        // LOGIN TESTS
        // =============================================================

        [Test]
        public async Task Login_WithCorrectCredentials_ShouldSucceed()
        {
            var hashedPw = BCrypt.Net.BCrypt.HashPassword("correctpass");
            var existingUser = new User
            {
                Id = 1, Username = "alice", Email = "alice@example.com",
                PasswordHash = hashedPw, Role = "User", CreatedAt = DateTime.UtcNow
            };

            _mockRepo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(existingUser);

            var dto = new LoginDto { Email = "alice@example.com", Password = "correctpass" };
            var (success, _, data) = await _authService.LoginAsync(dto);

            Assert.That(success, Is.True);
            Assert.That(data!.Username, Is.EqualTo("alice"));
        }

        [Test]
        public async Task Login_ShouldReturnJwtToken()
        {
            var hashedPw = BCrypt.Net.BCrypt.HashPassword("pass");
            var user = new User { Id = 1, Email = "a@b.com", PasswordHash = hashedPw, Role = "User" };
            _mockRepo.Setup(r => r.GetUserByEmailAsync("a@b.com")).ReturnsAsync(user);

            var dto = new LoginDto { Email = "a@b.com", Password = "pass" };
            var (success, _, data) = await _authService.LoginAsync(dto);

            Assert.That(success, Is.True);
            Assert.That(data!.Token, Is.Not.Null.And.Not.Empty,
                "Login response must include a JWT token");
        }

        [Test]
        public async Task Login_ShouldReturnCorrectRole()
        {
            var hashedPw = BCrypt.Net.BCrypt.HashPassword("pass");
            var adminUser = new User { Id = 2, Email = "admin@b.com", PasswordHash = hashedPw, Role = "Admin" };
            _mockRepo.Setup(r => r.GetUserByEmailAsync("admin@b.com")).ReturnsAsync(adminUser);

            var dto = new LoginDto { Email = "admin@b.com", Password = "pass" };
            var (_, _, data) = await _authService.LoginAsync(dto);

            Assert.That(data!.Role, Is.EqualTo("Admin"),
                "Login response should reflect the user's actual role from the DB");
        }

        [Test]
        public async Task Login_WithWrongPassword_ShouldFail()
        {
            var hashedPw = BCrypt.Net.BCrypt.HashPassword("correctpass");
            var user = new User { Id = 1, Email = "alice@example.com", PasswordHash = hashedPw };
            _mockRepo.Setup(r => r.GetUserByEmailAsync("alice@example.com")).ReturnsAsync(user);

            var dto = new LoginDto { Email = "alice@example.com", Password = "wrongpass" };
            var (success, _, data) = await _authService.LoginAsync(dto);

            Assert.That(success, Is.False);
            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task Login_WithUnknownEmail_ShouldFail_WithGenericMessage()
        {
            _mockRepo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var dto = new LoginDto { Email = "ghost@example.com", Password = "anypass" };
            var (success, message, _) = await _authService.LoginAsync(dto);

            Assert.That(success, Is.False);
            Assert.That(message, Is.EqualTo("Invalid email or password."),
                "Error must be generic to prevent user enumeration");
        }

        [Test]
        public async Task GetServiceStatus_ShouldReturnRunningMessage()
        {
            var result = await _authService.GetServiceStatusAsync();
            Assert.That(result, Does.Contain("running"));
        }
    }
}
