using Moq;
using NUnit.Framework;
using FileService.DTOs;
using FileService.Models;
using FileService.Repositories;
using FileService.Services;

namespace FileService.Tests
{
    [TestFixture]
    public class FileServiceTests
    {
        private Mock<IFileRepository> _repoMock  = null!;
        private Mock<ICacheService>   _cacheMock = null!;
        private IFileService          _service   = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock  = new Mock<IFileRepository>();
            _cacheMock = new Mock<ICacheService>();

            // Default: cache always misses → falls through to DB
            _service = new FileServiceImpl(_repoMock.Object, _cacheMock.Object);
        }

        // ── CREATE FILE ───────────────────────────────────────────────────

        [Test]
        public async Task CreateFileAsync_ValidInput_ReturnsSuccess()
        {
            var dto = new CreateFileDto { Name = "main.cs", Content = "// hello", ProjectId = 1 };

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<CodeFile>()))
                     .ReturnsAsync((CodeFile f) => { f.Id = 10; return f; });

            var (success, message, data) = await _service.CreateFileAsync(userId: 1, dto);

            Assert.That(success,       Is.True);
            Assert.That(data!.Id,      Is.EqualTo(10));
            Assert.That(data.IsFolder, Is.False);
            Assert.That(message,       Does.Contain("created"));
        }

        [Test]
        public async Task CreateFileAsync_InvalidatesTreeCache()
        {
            var dto = new CreateFileDto { Name = "x.cs", ProjectId = 5 };
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<CodeFile>()))
                     .ReturnsAsync((CodeFile f) => f);

            await _service.CreateFileAsync(1, dto);

            _cacheMock.Verify(c => c.RemoveAsync("file:tree:5"), Times.Once);
        }

        [Test]
        public async Task CreateFileAsync_TrimsName()
        {
            var dto = new CreateFileDto { Name = "  utils.cs  ", ProjectId = 1 };
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<CodeFile>()))
                     .ReturnsAsync((CodeFile f) => f);

            var (_, _, data) = await _service.CreateFileAsync(1, dto);

            Assert.That(data!.Name, Is.EqualTo("utils.cs"));
        }

        [Test]
        public async Task CreateFileAsync_ParentFolderZero_CreatesAtRoot()
        {
            var dto = new CreateFileDto { Name = "root.cs", ProjectId = 1, ParentFolderId = 0 };

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<CodeFile>()))
                     .ReturnsAsync((CodeFile f) => f);

            var (success, _, data) = await _service.CreateFileAsync(1, dto);

            Assert.That(success, Is.True);
            Assert.That(data!.ParentFolderId, Is.Null);
        }

        [Test]
        public async Task CreateFileAsync_MissingParentFolder_ReturnsFalse()
        {
            var dto = new CreateFileDto { Name = "main.cs", ProjectId = 1, ParentFolderId = 99 };
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((CodeFile?)null);

            var (success, message, data) = await _service.CreateFileAsync(1, dto);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("Parent folder not found"));
            Assert.That(data, Is.Null);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<CodeFile>()), Times.Never);
        }

        [Test]
        public async Task CreateFileAsync_ParentIsFile_ReturnsFalse()
        {
            var dto = new CreateFileDto { Name = "nested.cs", ProjectId = 1, ParentFolderId = 2 };
            _repoMock.Setup(r => r.GetByIdAsync(2))
                     .ReturnsAsync(new CodeFile { Id = 2, ProjectId = 1, IsFolder = false });

            var (success, message, _) = await _service.CreateFileAsync(1, dto);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("must be a folder"));
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<CodeFile>()), Times.Never);
        }

        [Test]
        public async Task CreateFileAsync_ValidParentFolder_CreatesNestedFile()
        {
            var dto = new CreateFileDto { Name = "nested.cs", ProjectId = 1, ParentFolderId = 2 };
            _repoMock.Setup(r => r.GetByIdAsync(2))
                     .ReturnsAsync(new CodeFile { Id = 2, ProjectId = 1, IsFolder = true });
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<CodeFile>()))
                     .ReturnsAsync((CodeFile f) => f);

            var (success, _, data) = await _service.CreateFileAsync(1, dto);

            Assert.That(success, Is.True);
            Assert.That(data!.ParentFolderId, Is.EqualTo(2));
        }

        // ── CREATE FOLDER ─────────────────────────────────────────────────

        [Test]
        public async Task CreateFolderAsync_SetsIsFolderTrue()
        {
            var dto = new CreateFolderDto { Name = "src", ProjectId = 1 };
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<CodeFile>()))
                     .ReturnsAsync((CodeFile f) => { f.Id = 3; return f; });

            var (success, _, data) = await _service.CreateFolderAsync(1, dto);

            Assert.That(success,       Is.True);
            Assert.That(data!.IsFolder, Is.True);
            Assert.That(data.Content,   Is.Null);
        }

        [Test]
        public async Task CreateFolderAsync_InvalidatesTreeCache()
        {
            var dto = new CreateFolderDto { Name = "src", ProjectId = 7 };
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<CodeFile>()))
                     .ReturnsAsync((CodeFile f) => f);

            await _service.CreateFolderAsync(1, dto);

            _cacheMock.Verify(c => c.RemoveAsync("file:tree:7"), Times.Once);
        }

        // ── UPDATE CONTENT ────────────────────────────────────────────────

        [Test]
        public async Task UpdateContentAsync_Owner_ReturnsSuccess()
        {
            var file = new CodeFile
            {
                Id = 1, IsFolder = false, Content = "old",
                CreatedByUserId = 1, ProjectId = 1,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<CodeFile>()))
                     .ReturnsAsync((CodeFile f) => f);

            var (success, _, data) = await _service.UpdateContentAsync(1, 1, new UpdateCodeFileDto { Content = "new" });

            Assert.That(success,       Is.True);
            Assert.That(data!.Content, Is.EqualTo("new"));
        }

        [Test]
        public async Task UpdateContentAsync_InvalidatesBothCacheKeys()
        {
            var file = new CodeFile
            {
                Id = 1, IsFolder = false, CreatedByUserId = 1, ProjectId = 3,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<CodeFile>()))
                     .ReturnsAsync((CodeFile f) => f);

            await _service.UpdateContentAsync(1, 1, new UpdateCodeFileDto { Content = "x" });

            _cacheMock.Verify(c => c.RemoveAsync("file:content:1"), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync("file:tree:3"),    Times.Once);
        }

        [Test]
        public async Task UpdateContentAsync_OnFolder_ReturnsFalse()
        {
            var folder = new CodeFile
            {
                Id = 1, IsFolder = true, CreatedByUserId = 1, ProjectId = 1,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(folder);

            var (success, message, _) = await _service.UpdateContentAsync(1, 1, new UpdateCodeFileDto { Content = "x" });

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("folder"));
        }

        [Test]
        public async Task UpdateContentAsync_WrongUser_ReturnsForbidden()
        {
            var file = new CodeFile
            {
                Id = 1, IsFolder = false, CreatedByUserId = 1, ProjectId = 1,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);

            var (success, message, _) = await _service.UpdateContentAsync(1, userId: 2, new UpdateCodeFileDto { Content = "x" });

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("access"));
        }

        // ── DELETE ────────────────────────────────────────────────────────

        [Test]
        public async Task DeleteAsync_File_InvalidatesBothCacheKeys()
        {
            var file = new CodeFile
            {
                Id = 2, IsFolder = false, CreatedByUserId = 1, ProjectId = 4,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(file);
            _repoMock.Setup(r => r.DeleteAsync(2)).ReturnsAsync(true);

            var (success, _) = await _service.DeleteAsync(2, 1);

            Assert.That(success, Is.True);
            _cacheMock.Verify(c => c.RemoveAsync("file:content:2"), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync("file:tree:4"),    Times.Once);
        }

        [Test]
        public async Task DeleteAsync_FolderWithChildren_ReturnsFalse()
        {
            var folder = new CodeFile
            {
                Id = 1, IsFolder = true, CreatedByUserId = 1, ProjectId = 1,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(folder);
            _repoMock.Setup(r => r.HasChildrenAsync(1)).ReturnsAsync(true);

            var (success, message) = await _service.DeleteAsync(1, 1);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("contents first"));
        }

        [Test]
        public async Task DeleteAsync_NotFound_ReturnsFalse()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((CodeFile?)null);

            var (success, message) = await _service.DeleteAsync(999, 1);

            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("not found"));
        }

        // ── GET TREE ──────────────────────────────────────────────────────

        [Test]
        public async Task GetFileTreeAsync_CacheHit_DoesNotCallDb()
        {
            var cached = new List<FileTreeNodeDto>
            {
                new() { Id = 1, Name = "src", IsFolder = true }
            };
            _cacheMock.Setup(c => c.GetAsync<List<FileTreeNodeDto>>("file:tree:1"))
                      .ReturnsAsync(cached);

            var (success, message, data) = await _service.GetFileTreeAsync(1, 1);

            Assert.That(success,      Is.True);
            Assert.That(data!.Count,  Is.EqualTo(1));
            Assert.That(message,      Does.Contain("[cache]"));
            _repoMock.Verify(r => r.GetAllByProjectAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task GetFileTreeAsync_CacheMiss_BuildsTreeAndSetsCache()
        {
            var files = new List<CodeFile>
            {
                new() { Id=1, Name="src",     IsFolder=true,  ParentFolderId=null, ProjectId=1, CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow },
                new() { Id=2, Name="main.cs", IsFolder=false, ParentFolderId=1,    ProjectId=1, CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow },
                new() { Id=3, Name="README",  IsFolder=false, ParentFolderId=null, ProjectId=1, CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow }
            };

            _cacheMock.Setup(c => c.GetAsync<List<FileTreeNodeDto>>("file:tree:1"))
                      .ReturnsAsync((List<FileTreeNodeDto>?)null);
            _repoMock.Setup(r => r.GetAllByProjectAsync(1)).ReturnsAsync(files);

            var (success, _, tree) = await _service.GetFileTreeAsync(1, 1);

            Assert.That(success,     Is.True);
            Assert.That(tree!.Count, Is.EqualTo(2));   // src + README at root

            var src = tree.First(n => n.Name == "src");
            Assert.That(src.IsFolder,          Is.True);
            Assert.That(src.Children.Count,    Is.EqualTo(1));
            Assert.That(src.Children[0].Name,  Is.EqualTo("main.cs"));

            // Must save to cache
            _cacheMock.Verify(c => c.SetAsync("file:tree:1", It.IsAny<List<FileTreeNodeDto>>()), Times.Once);
        }
    }
}
