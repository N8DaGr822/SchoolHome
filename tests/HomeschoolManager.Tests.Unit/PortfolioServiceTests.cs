using System.Text;
using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class PortfolioServiceTests
{
    [Fact]
    public async Task CreateItemAsync_StoresUploadedFileMetadata()
    {
        var repository = new FakePortfolioRepository([]);
        var storage = new FakePortfolioFileStorage();
        var service = new PortfolioService(repository, storage);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        var created = await service.CreateItemAsync(
            new PortfolioItem { StudentId = 1, SubjectId = 1, Title = "Essay Scan", Type = PortfolioItemType.Pdf },
            new PortfolioUpload(stream, "essay.pdf", "application/pdf"));

        Assert.Equal("essay.pdf", created.OriginalFileName);
        Assert.False(string.IsNullOrWhiteSpace(created.StoredFilePath));
        Assert.Equal("application/pdf", created.ContentType);
        Assert.Equal(5, created.FileSizeBytes);
    }

    [Fact]
    public async Task CreateItemAsync_RequiresUrlForLinkItems()
    {
        var service = new PortfolioService(new FakePortfolioRepository([]), new FakePortfolioFileStorage());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateItemAsync(new PortfolioItem
            {
                StudentId = 1,
                SubjectId = 1,
                Title = "Video Project",
                Type = PortfolioItemType.Video,
                ExternalUrl = "  "
            }));

        Assert.Contains("URL", exception.Message);
    }

    [Fact]
    public async Task CreateItemAsync_RequiresTitle()
    {
        var service = new PortfolioService(new FakePortfolioRepository([]), new FakePortfolioFileStorage());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateItemAsync(new PortfolioItem { StudentId = 1, SubjectId = 1, Title = " " }));
    }

    [Fact]
    public async Task UpdateItemAsync_KeepsExistingFileWhenNoNewUpload()
    {
        var repository = new FakePortfolioRepository([
            new PortfolioItem
            {
                Id = 1,
                StudentId = 1,
                SubjectId = 1,
                Title = "Original",
                OriginalFileName = "report.pdf",
                StoredFileName = "abc.pdf",
                StoredFilePath = "/files/abc.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 1024,
                CreatedAt = new DateTime(2026, 1, 1)
            }
        ]);
        var storage = new FakePortfolioFileStorage();
        var service = new PortfolioService(repository, storage);

        var updated = await service.UpdateItemAsync(new PortfolioItem
        {
            Id = 1,
            StudentId = 1,
            SubjectId = 1,
            Title = "Renamed"
        });

        Assert.Equal("report.pdf", updated.OriginalFileName);
        Assert.Equal("/files/abc.pdf", updated.StoredFilePath);
        Assert.Equal(1024, updated.FileSizeBytes);
        Assert.Equal(new DateTime(2026, 1, 1), updated.CreatedAt);
        Assert.Empty(storage.DeletedPaths);
    }

    [Fact]
    public async Task UpdateItemAsync_ReplacesStoredFileWhenNewUploadProvided()
    {
        var repository = new FakePortfolioRepository([
            new PortfolioItem
            {
                Id = 1,
                StudentId = 1,
                SubjectId = 1,
                Title = "Original",
                StoredFilePath = "/files/old.pdf",
                CreatedAt = new DateTime(2026, 1, 1)
            }
        ]);
        var storage = new FakePortfolioFileStorage();
        var service = new PortfolioService(repository, storage);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("new content"));
        var updated = await service.UpdateItemAsync(
            new PortfolioItem { Id = 1, StudentId = 1, SubjectId = 1, Title = "Replaced" },
            new PortfolioUpload(stream, "new.pdf", "application/pdf"));

        Assert.Contains("/files/old.pdf", storage.DeletedPaths);
        Assert.Equal("new.pdf", updated.OriginalFileName);
    }

    [Fact]
    public async Task DeleteItemAsync_RemovesStoredFileAndItem()
    {
        var repository = new FakePortfolioRepository([
            new PortfolioItem
            {
                Id = 1,
                StudentId = 1,
                SubjectId = 1,
                Title = "To Delete",
                StoredFilePath = "/files/doomed.pdf"
            }
        ]);
        var storage = new FakePortfolioFileStorage();
        var service = new PortfolioService(repository, storage);

        await service.DeleteItemAsync(1);

        Assert.Contains("/files/doomed.pdf", storage.DeletedPaths);
        Assert.Null(await repository.GetByIdAsync(1));
    }

    private sealed class FakePortfolioRepository : IPortfolioRepository
    {
        private readonly List<PortfolioItem> _items;

        public FakePortfolioRepository(IEnumerable<PortfolioItem> items)
        {
            _items = items.ToList();
        }

        public Task<PortfolioItem?> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault(i => i.Id == id));
        public Task<IEnumerable<PortfolioItem>> GetAllAsync() => Task.FromResult<IEnumerable<PortfolioItem>>(_items);

        public Task<PortfolioItem> AddAsync(PortfolioItem entity)
        {
            entity.Id = _items.Select(i => i.Id).DefaultIfEmpty(0).Max() + 1;
            _items.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(PortfolioItem entity)
        {
            var index = _items.FindIndex(i => i.Id == entity.Id);
            if (index >= 0)
            {
                _items[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _items.RemoveAll(i => i.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id) => Task.FromResult(_items.Any(i => i.Id == id));

        public Task<IEnumerable<PortfolioItem>> GetFilteredAsync(PortfolioFilter filter) =>
            Task.FromResult<IEnumerable<PortfolioItem>>(_items
                .Where(i => !filter.StudentId.HasValue || i.StudentId == filter.StudentId.Value)
                .Where(i => !filter.BestWorkOnly || i.IsBestWork));

        public Task<IEnumerable<PortfolioItem>> GetByStudentIdAsync(int studentId) =>
            Task.FromResult<IEnumerable<PortfolioItem>>(_items.Where(i => i.StudentId == studentId));

        public Task<IEnumerable<PortfolioItem>> GetByAssignmentIdAsync(int assignmentId) =>
            Task.FromResult<IEnumerable<PortfolioItem>>(_items.Where(i => i.AssignmentId == assignmentId));

        public Task<IEnumerable<PortfolioItem>> GetByLessonPlanIdAsync(int lessonPlanId) =>
            Task.FromResult<IEnumerable<PortfolioItem>>(_items.Where(i => i.LessonPlanId == lessonPlanId));
    }

    private sealed class FakePortfolioFileStorage : IPortfolioFileStorage
    {
        public List<string> DeletedPaths { get; } = [];

        public string StorageRoot => "/files";

        public async Task<StoredPortfolioFile> SaveAsync(Stream stream, string fileName, string contentType)
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
            return new StoredPortfolioFile(
                Path.GetFileName(fileName),
                storedFileName,
                Path.Combine(StorageRoot, storedFileName),
                contentType,
                buffer.Length);
        }

        public Task DeleteAsync(string storedFilePath)
        {
            if (!string.IsNullOrWhiteSpace(storedFilePath))
            {
                DeletedPaths.Add(storedFilePath);
            }

            return Task.CompletedTask;
        }
    }
}
