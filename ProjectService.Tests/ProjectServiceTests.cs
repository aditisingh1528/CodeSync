using Moq;
using NUnit.Framework;
using ProjectService.DTOs;
using ProjectService.Models;
using ProjectService.Repositories;
using ProjectService.Services;

namespace ProjectService.Tests
{
    /// <summary>
    /// PROJECTSERVICEIMPL TESTS — UC-6 UPDATED
    /// =========================================
    /// ICacheService is MOCKED — no real Redis needed.
    /// 
    /// Cache mock behaviour in tests:
    ///   GetAsync  → returns null by default (simulates cache miss → goes to DB)
    ///   SetAsync  → does nothing (we just verify it was called)
    ///   RemoveAsync / RemoveByPrefixAsync → does nothing (verify called)
    ///
    /// Tests verify BOTH:
    ///   1. Correct business logic (same as UC-5)
    ///   2. Cache is SET on reads and INVALIDATED on writes
    /// </summary>
    [TestFixture]
    public class ProjectServiceTests
    {
        private Mock<IProjectRepository> _repoMock  = null!;
        private Mock<ICacheService>      _cacheMock = null!;
        private IProjectService          _service   = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock  = new Mock<IProjectRepository>();
            _cacheMock = new Mock<ICacheService>();

            // Default: cache always misses (returns null) → falls through to DB
            _cacheMock.Setup(c => c.GetAsync<It.IsAnyType>(It.IsAny<string>()))
                      .ReturnsAsync((object?)null);

            _service = new ProjectServiceImpl(_repoMock.Object, _cacheMock.Object);
        }

        // =================================================================
        // CREATE TESTS
        // =================================================================

        [Test]
        public async Task CreateAsync_ValidInput_ReturnsSuccess()
        {
            var dto    = new CreateProjectDto { Name = "My App", Description = "Test desc" };
            var userId = 1;

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Project>()))
                     .ReturnsAsync((Project p) => { p.Id = 10; return p; });

            var (success, message, data) = await _service.CreateAsync(userId, dto);

            Assert.That(success,     Is.True);
            Assert.That(data,        Is.Not.Null);
            Assert.That(data!.Name,  Is.EqualTo("My App"));
            Assert.That(data.UserId, Is.EqualTo(userId));
            Assert.That(data.Id,     Is.EqualTo(10));
            Assert.That(message,     Does.Contain("created"));
        }

        [Test]
        public async Task CreateAsync_InvalidatesUserCache()
        {
            // Create should wipe the user's "all projects" cache
            var dto    = new CreateProjectDto { Name = "X", Description = "" };
            var userId = 7;

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Project>()))
                     .ReturnsAsync((Project p) => p);

            await _service.CreateAsync(userId, dto);

            // Must have called RemoveByPrefix with the user's prefix
            _cacheMock.Verify(
                c => c.RemoveByPrefixAsync($"project:user:{userId}:"),
                Times.Once);
        }

        [Test]
        public async Task CreateAsync_TrimsWhitespaceFromName()
        {
            var dto    = new CreateProjectDto { Name = "  My Project  ", Description = "  desc  " };
            var userId = 1;

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<Project>()))
                     .ReturnsAsync((Project p) => p);

            var (_, _, data) = await _service.CreateAsync(userId, dto);

            Assert.That(data!.Name,       Is.EqualTo("My Project"));
            Assert.That(data.Description, Is.EqualTo("desc"));
        }

        // =================================================================
        // GET ALL TESTS
        // =================================================================

        [Test]
        public async Task GetAllByUserAsync_CacheMiss_FetchesFromDbAndSetsCache()
        {
            var userId   = 3;
            var projects = new List<Project>
            {
                new() { Id = 1, Name = "P1", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { Id = 2, Name = "P2", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            // Cache miss
            _cacheMock.Setup(c => c.GetAsync<List<ProjectResponseDto>>("project:user:3:all"))
                      .ReturnsAsync((List<ProjectResponseDto>?)null);

            _repoMock.Setup(r => r.GetAllByUserIdAsync(userId))
                     .ReturnsAsync(projects);

            var (success, message, data) = await _service.GetAllByUserAsync(userId);

            Assert.That(success,       Is.True);
            Assert.That(data!.Count(), Is.EqualTo(2));

            // Must save to cache after DB fetch
            _cacheMock.Verify(
                c => c.SetAsync("project:user:3:all", It.IsAny<List<ProjectResponseDto>>()),
                Times.Once);

            // Message should NOT say [cache]
            Assert.That(message, Does.Not.Contain("[cache]"));
        }

        [Test]
        public async Task GetAllByUserAsync_CacheHit_DoesNotCallDb()
        {
            var userId = 3;
            var cached = new List<ProjectResponseDto>
            {
                new() { Id = 1, Name = "Cached", UserId = userId }
            };

            // Cache HIT
            _cacheMock.Setup(c => c.GetAsync<List<ProjectResponseDto>>("project:user:3:all"))
                      .ReturnsAsync(cached);

            var (success, message, data) = await _service.GetAllByUserAsync(userId);

            Assert.That(success,       Is.True);
            Assert.That(data!.Count(), Is.EqualTo(1));

            // DB must NOT be called when cache hits
            _repoMock.Verify(r => r.GetAllByUserIdAsync(It.IsAny<int>()), Times.Never);

            // Message should indicate cache hit
            Assert.That(message, Does.Contain("[cache]"));
        }

        // =================================================================
        // GET BY ID TESTS
        // =================================================================

        [Test]
        public async Task GetByIdAsync_CacheMiss_FetchesFromDbAndSetsCache()
        {
            var userId  = 1;
            var project = new Project { Id = 5, Name = "MyProj", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            _cacheMock.Setup(c => c.GetAsync<ProjectResponseDto>("project:user:1:5"))
                      .ReturnsAsync((ProjectResponseDto?)null);

            _repoMock.Setup(r => r.GetByIdAsync(5))
                     .ReturnsAsync(project);

            var (success, _, data) = await _service.GetByIdAsync(5, userId);

            Assert.That(success,   Is.True);
            Assert.That(data!.Id,  Is.EqualTo(5));

            // Must have saved to cache
            _cacheMock.Verify(
                c => c.SetAsync("project:user:1:5", It.IsAny<ProjectResponseDto>()),
                Times.Once);
        }

        [Test]
        public async Task GetByIdAsync_CacheHit_DoesNotCallDb()
        {
            var userId = 1;
            var cached = new ProjectResponseDto { Id = 5, Name = "Cached", UserId = userId };

            _cacheMock.Setup(c => c.GetAsync<ProjectResponseDto>("project:user:1:5"))
                      .ReturnsAsync(cached);

            var (success, _, data) = await _service.GetByIdAsync(5, userId);

            Assert.That(success,  Is.True);
            Assert.That(data!.Id, Is.EqualTo(5));

            // DB must NOT be called
            _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task GetByIdAsync_NotFound_ReturnsFalse()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((Project?)null);

            var (success, message, data) = await _service.GetByIdAsync(999, 1);

            Assert.That(success, Is.False);
            Assert.That(data,    Is.Null);
            Assert.That(message, Does.Contain("not found"));
        }

        [Test]
        public async Task GetByIdAsync_WrongUser_ReturnsFalse()
        {
            var project = new Project { Id = 1, Name = "P", UserId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            _repoMock.Setup(r => r.GetByIdAsync(1))
                     .ReturnsAsync(project);

            var (success, message, data) = await _service.GetByIdAsync(1, userId: 2);

            Assert.That(success, Is.False);
            Assert.That(data,    Is.Null);
            Assert.That(message, Does.Contain("access"));
        }

        // =================================================================
        // UPDATE TESTS
        // =================================================================

        [Test]
        public async Task UpdateAsync_OwnerUpdates_InvalidatesBothKeys()
        {
            var userId  = 1;
            var project = new Project { Id = 1, Name = "Old", Description = "Old desc", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var dto     = new UpdateProjectDto { Name = "New Name", Description = "New desc" };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Project>()))
                     .ReturnsAsync((Project p) => p);

            var (success, _, data) = await _service.UpdateAsync(1, userId, dto);

            Assert.That(success,          Is.True);
            Assert.That(data!.Name,       Is.EqualTo("New Name"));
            Assert.That(data.Description, Is.EqualTo("New desc"));

            // Must invalidate the specific project key
            _cacheMock.Verify(c => c.RemoveAsync("project:user:1:1"), Times.Once);
            // Must invalidate the "all" list
            _cacheMock.Verify(c => c.RemoveByPrefixAsync("project:user:1:"), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_WrongUser_ReturnsFalse()
        {
            var project = new Project { Id = 1, UserId = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var dto     = new UpdateProjectDto { Name = "Hack", Description = "" };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

            var (success, message, data) = await _service.UpdateAsync(1, userId: 2, dto);

            Assert.That(success, Is.False);
            Assert.That(data,    Is.Null);
            Assert.That(message, Does.Contain("access"));
        }

        [Test]
        public async Task UpdateAsync_BumpsUpdatedAt()
        {
            var oldTime = DateTime.UtcNow.AddMinutes(-5);
            var project = new Project { Id = 1, UserId = 1, Name = "P", UpdatedAt = oldTime, CreatedAt = oldTime };
            var dto     = new UpdateProjectDto { Name = "New", Description = "" };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Project>()))
                     .ReturnsAsync((Project p) => p);

            var (_, _, data) = await _service.UpdateAsync(1, 1, dto);

            Assert.That(data!.UpdatedAt, Is.GreaterThan(oldTime));
        }

        // =================================================================
        // DELETE TESTS
        // =================================================================

        [Test]
        public async Task DeleteAsync_OwnerDeletes_InvalidatesBothKeys()
        {
            var project = new Project { Id = 1, UserId = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var (success, message) = await _service.DeleteAsync(1, userId: 1);

            Assert.That(success, Is.True);
            Assert.That(message, Does.Contain("deleted"));

            // Must invalidate specific key
            _cacheMock.Verify(c => c.RemoveAsync("project:user:1:1"), Times.Once);
            // Must invalidate the list
            _cacheMock.Verify(c => c.RemoveByPrefixAsync("project:user:1:"), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_WrongUser_ReturnsFalse_NoCacheOps()
        {
            var project = new Project { Id = 1, UserId = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

            var (success, message) = await _service.DeleteAsync(1, userId: 2);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("access"));

            // DB delete must NOT be called
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
            // Cache invalidation must NOT be called either
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()),         Times.Never);
            _cacheMock.Verify(c => c.RemoveByPrefixAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task DeleteAsync_NotFound_ReturnsFalse()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((Project?)null);

            var (success, message) = await _service.DeleteAsync(999, 1);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("not found"));
        }
    }
}
