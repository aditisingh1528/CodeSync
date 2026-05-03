using Moq;
using NUnit.Framework;
using ProjectService.DTOs;
using ProjectService.Models;
using ProjectService.Repositories;
using ProjectService.Services;

namespace ProjectService.Tests
{
    /// <summary>
    /// PROJECTSERVICEIMPL TESTS
    /// =========================
    /// Tests the business logic layer in isolation.
    /// The repository is MOCKED — no real database is needed.
    ///
    /// Test naming convention:
    ///   MethodName_Scenario_ExpectedResult
    /// </summary>
    [TestFixture]
    public class ProjectServiceTests
    {
        private Mock<IProjectRepository> _repoMock = null!;
        private IProjectService          _service  = null!;

        // ── SETUP ─────────────────────────────────────────────────────────
        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IProjectRepository>();
            _service  = new ProjectServiceImpl(_repoMock.Object);
        }

        // =================================================================
        // CREATE TESTS
        // =================================================================

        [Test]
        public async Task CreateAsync_ValidInput_ReturnsSuccess()
        {
            // Arrange
            var dto = new CreateProjectDto { Name = "My App", Description = "Test desc" };
            var userId = 1;

            // Mock: repo returns the project with Id assigned
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Project>()))
                     .ReturnsAsync((Project p) => { p.Id = 10; return p; });

            // Act
            var (success, message, data) = await _service.CreateAsync(userId, dto);

            // Assert
            Assert.That(success,        Is.True);
            Assert.That(data,           Is.Not.Null);
            Assert.That(data!.Name,     Is.EqualTo("My App"));
            Assert.That(data.UserId,    Is.EqualTo(userId));
            Assert.That(data.Id,        Is.EqualTo(10));
            Assert.That(message,        Does.Contain("created"));
        }

        [Test]
        public async Task CreateAsync_SetsCreatedAtAndUpdatedAt()
        {
            // Arrange
            var dto    = new CreateProjectDto { Name = "Test", Description = "" };
            var userId = 5;
            var before = DateTime.UtcNow.AddSeconds(-1);

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Project>()))
                     .ReturnsAsync((Project p) => p);

            // Act
            var (_, _, data) = await _service.CreateAsync(userId, dto);

            // Assert — timestamps must be set and recent
            Assert.That(data!.CreatedAt, Is.GreaterThan(before));
            Assert.That(data.UpdatedAt,  Is.GreaterThan(before));
        }

        [Test]
        public async Task CreateAsync_TrimsWhitespaceFromName()
        {
            // Arrange
            var dto    = new CreateProjectDto { Name = "  My Project  ", Description = "  desc  " };
            var userId = 1;

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Project>()))
                     .ReturnsAsync((Project p) => p);

            // Act
            var (_, _, data) = await _service.CreateAsync(userId, dto);

            // Assert
            Assert.That(data!.Name,        Is.EqualTo("My Project"));
            Assert.That(data.Description,  Is.EqualTo("desc"));
        }

        // =================================================================
        // GET ALL TESTS
        // =================================================================

        [Test]
        public async Task GetAllByUserAsync_ReturnsOnlyUserProjects()
        {
            // Arrange
            var userId = 3;
            var projects = new List<Project>
            {
                new() { Id = 1, Name = "P1", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Id = 2, Name = "P2", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            _repoMock.Setup(r => r.GetAllByUserIdAsync(userId))
                     .ReturnsAsync(projects);

            // Act
            var (success, message, data) = await _service.GetAllByUserAsync(userId);

            // Assert
            Assert.That(success,           Is.True);
            Assert.That(data,              Is.Not.Null);
            Assert.That(data!.Count(),     Is.EqualTo(2));
            Assert.That(data.All(p => p.UserId == userId), Is.True);
        }

        [Test]
        public async Task GetAllByUserAsync_NoProjects_ReturnsEmptyList()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllByUserIdAsync(It.IsAny<int>()))
                     .ReturnsAsync(new List<Project>());

            // Act
            var (success, _, data) = await _service.GetAllByUserAsync(99);

            // Assert
            Assert.That(success,       Is.True);
            Assert.That(data,          Is.Not.Null);
            Assert.That(data!.Count(), Is.EqualTo(0));
        }

        // =================================================================
        // GET BY ID TESTS
        // =================================================================

        [Test]
        public async Task GetByIdAsync_OwnerRequests_ReturnsProject()
        {
            // Arrange
            var userId  = 1;
            var project = new Project { Id = 5, Name = "MyProj", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            _repoMock.Setup(r => r.GetByIdAsync(5))
                     .ReturnsAsync(project);

            // Act
            var (success, _, data) = await _service.GetByIdAsync(5, userId);

            // Assert
            Assert.That(success,     Is.True);
            Assert.That(data,        Is.Not.Null);
            Assert.That(data!.Id,    Is.EqualTo(5));
            Assert.That(data.Name,   Is.EqualTo("MyProj"));
        }

        [Test]
        public async Task GetByIdAsync_NotFound_ReturnsFalse()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((Project?)null);

            // Act
            var (success, message, data) = await _service.GetByIdAsync(999, 1);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(data,    Is.Null);
            Assert.That(message, Does.Contain("not found"));
        }

        [Test]
        public async Task GetByIdAsync_WrongUser_ReturnsFalse()
        {
            // Arrange — project belongs to userId=1, but userId=2 is requesting
            var project = new Project { Id = 1, Name = "P", UserId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            _repoMock.Setup(r => r.GetByIdAsync(1))
                     .ReturnsAsync(project);

            // Act
            var (success, message, data) = await _service.GetByIdAsync(1, userId: 2);

            // Assert — ownership check must fail
            Assert.That(success, Is.False);
            Assert.That(data,    Is.Null);
            Assert.That(message, Does.Contain("access"));
        }

        // =================================================================
        // UPDATE TESTS
        // =================================================================

        [Test]
        public async Task UpdateAsync_OwnerUpdates_ReturnsUpdatedProject()
        {
            // Arrange
            var userId  = 1;
            var project = new Project { Id = 1, Name = "Old", Description = "Old desc", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var dto     = new UpdateProjectDto { Name = "New Name", Description = "New desc" };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Project>()))
                     .ReturnsAsync((Project p) => p);

            // Act
            var (success, _, data) = await _service.UpdateAsync(1, userId, dto);

            // Assert
            Assert.That(success,          Is.True);
            Assert.That(data!.Name,       Is.EqualTo("New Name"));
            Assert.That(data.Description, Is.EqualTo("New desc"));
        }

        [Test]
        public async Task UpdateAsync_WrongUser_ReturnsFalse()
        {
            // Arrange
            var project = new Project { Id = 1, UserId = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var dto     = new UpdateProjectDto { Name = "Hack", Description = "" };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

            // Act — userId=2 tries to update userId=1's project
            var (success, message, data) = await _service.UpdateAsync(1, userId: 2, dto);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(data,    Is.Null);
            Assert.That(message, Does.Contain("access"));
        }

        [Test]
        public async Task UpdateAsync_NotFound_ReturnsFalse()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((Project?)null);

            var dto = new UpdateProjectDto { Name = "X", Description = "" };

            // Act
            var (success, message, _) = await _service.UpdateAsync(999, 1, dto);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("not found"));
        }

        [Test]
        public async Task UpdateAsync_BumpsUpdatedAt()
        {
            // Arrange
            var oldTime = DateTime.UtcNow.AddMinutes(-5);
            var project = new Project { Id = 1, UserId = 1, Name = "P", UpdatedAt = oldTime, CreatedAt = oldTime };
            var dto     = new UpdateProjectDto { Name = "New", Description = "" };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Project>()))
                     .ReturnsAsync((Project p) => p);

            // Act
            var (_, _, data) = await _service.UpdateAsync(1, 1, dto);

            // Assert — UpdatedAt must be newer than the old value
            Assert.That(data!.UpdatedAt, Is.GreaterThan(oldTime));
        }

        // =================================================================
        // DELETE TESTS
        // =================================================================

        [Test]
        public async Task DeleteAsync_OwnerDeletes_ReturnsSuccess()
        {
            // Arrange
            var project = new Project { Id = 1, UserId = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            var (success, message) = await _service.DeleteAsync(1, userId: 1);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(message, Does.Contain("deleted"));
        }

        [Test]
        public async Task DeleteAsync_WrongUser_ReturnsFalse()
        {
            // Arrange — project belongs to user 1, user 2 tries to delete
            var project = new Project { Id = 1, UserId = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

            // Act
            var (success, message) = await _service.DeleteAsync(1, userId: 2);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("access"));

            // Verify repo.DeleteAsync was NEVER called — ownership blocked it
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task DeleteAsync_NotFound_ReturnsFalse()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((Project?)null);

            // Act
            var (success, message) = await _service.DeleteAsync(999, 1);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("not found"));
        }
    }
}
