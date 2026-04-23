using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using AuthService.Services;
using Moq;
using NUnit.Framework;

namespace AuthService.Tests
{
    // ---------------------------------------------------------------
    // Unit tests for AuthServiceImpl
    //
    // We use Moq to fake the repository so these tests never touch
    // a real database - they run fast and in isolation.
    // ---------------------------------------------------------------
    [TestFixture]
    public class AuthServiceImplTests
    {
        private Mock<IUserRepository> _mockRepo = null!;
        private AuthServiceImpl _authService = null!;

        // SetUp runs before every single test
        [SetUp]
        public void SetUp()
        {
            _mockRepo    = new Mock<IUserRepository>();
            _authService = new AuthServiceImpl(_mockRepo.Object);
        }

        // =============================================================
        // REGISTER TESTS
        // =============================================================

        [Test]
        public async Task Register_WithNewEmailAndUsername_ShouldSucceed()
        {
            // Arrange: email and username are both free
            _mockRepo.Setup(r => r.EmailExistsAsync("alice@example.com")).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UsernameExistsAsync("alice")).ReturnsAsync(false);
            _mockRepo.Setup(r => r.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var dto = new RegisterDto
            {
                Username = "alice",
                Email    = "alice@example.com",
                Password = "secret123"
            };

            // Act
            var (success, message, data) = await _authService.RegisterAsync(dto);

            // Assert
            Assert.That(success, Is.True, "Registration should succeed");
            Assert.That(data, Is.Not.Null);
            Assert.That(data!.Email, Is.EqualTo("alice@example.com"));
            Assert.That(data.Username, Is.EqualTo("alice"));
        }

        [Test]
        public async Task Register_WithDuplicateEmail_ShouldFail()
        {
            // Arrange: email is already taken
            _mockRepo.Setup(r => r.EmailExistsAsync("taken@example.com")).ReturnsAsync(true);

            var dto = new RegisterDto
            {
                Username = "bob",
                Email    = "taken@example.com",
                Password = "pass123"
            };

            // Act
            var (success, message, data) = await _authService.RegisterAsync(dto);

            // Assert
            Assert.That(success, Is.False, "Should fail when email is already registered");
            Assert.That(message, Does.Contain("Email"), "Error message should mention email");
            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task Register_WithDuplicateUsername_ShouldFail()
        {
            // Arrange: email is free, but username is taken
            _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UsernameExistsAsync("alice")).ReturnsAsync(true);

            var dto = new RegisterDto
            {
                Username = "alice",
                Email    = "new@example.com",
                Password = "pass123"
            };

            // Act
            var (success, message, data) = await _authService.RegisterAsync(dto);

            // Assert
            Assert.That(success, Is.False, "Should fail when username is already taken");
            Assert.That(message, Does.Contain("Username"), "Error message should mention username");
        }

        [Test]
        public async Task Register_ShouldNotStorePasswordInPlainText()
        {
            // Arrange
            User? savedUser = null;
            _mockRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.AddUserAsync(It.IsAny<User>()))
                     .Callback<User>(u => savedUser = u)  // capture the user object
                     .Returns(Task.CompletedTask);

            var dto = new RegisterDto
            {
                Username = "carol",
                Email    = "carol@example.com",
                Password = "mypassword"
            };

            // Act
            await _authService.RegisterAsync(dto);

            // Assert: the stored hash should NOT be the raw password
            Assert.That(savedUser, Is.Not.Null);
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
            // Arrange: create a user whose password is already hashed
            var hashedPw = BCrypt.Net.BCrypt.HashPassword("correctpass");
            var existingUser = new User
            {
                Id           = 1,
                Username     = "alice",
                Email        = "alice@example.com",
                PasswordHash = hashedPw,
                CreatedAt    = DateTime.UtcNow
            };

            _mockRepo.Setup(r => r.GetUserByEmailAsync("alice@example.com"))
                     .ReturnsAsync(existingUser);

            var dto = new LoginDto { Email = "alice@example.com", Password = "correctpass" };

            // Act
            var (success, message, data) = await _authService.LoginAsync(dto);

            // Assert
            Assert.That(success, Is.True, "Login should succeed with correct credentials");
            Assert.That(data, Is.Not.Null);
            Assert.That(data!.Username, Is.EqualTo("alice"));
        }

        [Test]
        public async Task Login_WithWrongPassword_ShouldFail()
        {
            var hashedPw = BCrypt.Net.BCrypt.HashPassword("correctpass");
            var existingUser = new User
            {
                Id           = 1,
                Email        = "alice@example.com",
                PasswordHash = hashedPw
            };

            _mockRepo.Setup(r => r.GetUserByEmailAsync("alice@example.com"))
                     .ReturnsAsync(existingUser);

            var dto = new LoginDto { Email = "alice@example.com", Password = "wrongpass" };

            // Act
            var (success, message, data) = await _authService.LoginAsync(dto);

            // Assert
            Assert.That(success, Is.False, "Login should fail with wrong password");
            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task Login_WithUnknownEmail_ShouldFail()
        {
            // Arrange: no user found for this email
            _mockRepo.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>()))
                     .ReturnsAsync((User?)null);

            var dto = new LoginDto { Email = "ghost@example.com", Password = "anypass" };

            // Act
            var (success, message, data) = await _authService.LoginAsync(dto);

            // Assert
            Assert.That(success, Is.False, "Login should fail if email doesn't exist");
            // Security: both "wrong email" and "wrong password" should return the same message
            Assert.That(message, Is.EqualTo("Invalid email or password."),
                "Error message should not reveal whether email or password was wrong");
        }

        [Test]
        public async Task GetServiceStatus_ShouldReturnRunningMessage()
        {
            // This covers the UC-1 status endpoint
            var result = await _authService.GetServiceStatusAsync();
            Assert.That(result, Does.Contain("running"), "Status message should say the service is running");
        }
    }
}
