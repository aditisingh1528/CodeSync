using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using AuthService.Services;
using Moq;
using NUnit.Framework;

namespace AuthService.Tests
{
    [TestFixture]
    public class AdminServiceTests
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
        }

        // =============================================================
        // UPDATE ROLE TESTS
        // =============================================================

        [Test]
        public async Task UpdateUserRole_WithValidUser_ShouldSucceed()
        {
            _mockRepo.Setup(r => r.UpdateUserRoleAsync(5, "Admin")).ReturnsAsync(true);
            var dto = new UpdateRoleDto { UserId = 5, Role = "Admin" };

            var (success, message) = await _authService.UpdateUserRoleAsync(dto);

            Assert.That(success, Is.True);
            Assert.That(message, Does.Contain("5"));
            Assert.That(message, Does.Contain("Admin"));
        }

        [Test]
        public async Task UpdateUserRole_WithNonExistentUser_ShouldFail()
        {
            _mockRepo.Setup(r => r.UpdateUserRoleAsync(999, It.IsAny<string>())).ReturnsAsync(false);
            var dto = new UpdateRoleDto { UserId = 999, Role = "Admin" };

            var (success, message) = await _authService.UpdateUserRoleAsync(dto);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("not found").IgnoreCase);
        }

        [Test]
        public async Task UpdateUserRole_ToDemoteAdmin_ShouldSucceed()
        {
            _mockRepo.Setup(r => r.UpdateUserRoleAsync(3, "User")).ReturnsAsync(true);
            var dto = new UpdateRoleDto { UserId = 3, Role = "User" };

            var (success, message) = await _authService.UpdateUserRoleAsync(dto);

            Assert.That(success, Is.True);
            Assert.That(message, Does.Contain("User"));
        }

        [Test]
        public async Task UpdateUserRole_ShouldCallRepositoryExactlyOnce()
        {
            _mockRepo.Setup(r => r.UpdateUserRoleAsync(It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(true);
            var dto = new UpdateRoleDto { UserId = 1, Role = "Admin" };
            await _authService.UpdateUserRoleAsync(dto);

            _mockRepo.Verify(r => r.UpdateUserRoleAsync(1, "Admin"), Times.Once);
        }

        [Test]
        public void UpdateRoleDto_WithInvalidRole_ShouldFailValidation()
        {
            var dto     = new UpdateRoleDto { UserId = 1, Role = "SuperAdmin" };
            var results = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var ctx     = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, ctx, results, true);

            Assert.That(results.Any(r => r.MemberNames.Contains("Role")), Is.True);
        }

        [Test]
        public void UpdateRoleDto_WithValidRoles_ShouldPassValidation()
        {
            foreach (var role in new[] { "User", "Admin" })
            {
                var dto     = new UpdateRoleDto { UserId = 1, Role = role };
                var results = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
                var ctx     = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
                System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, ctx, results, true);

                Assert.That(results, Is.Empty, $"Role '{role}' should be valid");
            }
        }

        // =============================================================
        // GET ALL USERS — verify no PasswordHash in response
        // =============================================================

        [Test]
        public async Task GetAllUsers_ShouldReturnUserSummaryDto_NotRawUser()
        {
            var users = new List<User>
            {
                new User { Id = 1, Username = "alice", Email = "a@b.com", Role = "User",  PasswordHash = "secret_hash_1", CreatedAt = DateTime.UtcNow },
                new User { Id = 2, Username = "bob",   Email = "b@b.com", Role = "Admin", PasswordHash = "secret_hash_2", CreatedAt = DateTime.UtcNow }
            };
            _mockRepo.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(users);

            var result = (await _authService.GetAllUsersAsync()).ToList();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0], Is.TypeOf<UserSummaryDto>(),
                "Must return UserSummaryDto, not raw User model");

            // Confirm PasswordHash property does not exist on the DTO
            var props = typeof(UserSummaryDto).GetProperties().Select(p => p.Name);
            Assert.That(props, Does.Not.Contain("PasswordHash"),
                "UserSummaryDto must NOT expose PasswordHash");
        }
    }
}
