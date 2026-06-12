using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class ParentNoteServiceTests
{
    [Fact]
    public async Task CreateNoteAsync_TrimsFieldsAndStampsCreatedAt()
    {
        var repository = new FakeParentNoteRepository([]);
        var service = new ParentNoteService(repository);

        var created = await service.CreateNoteAsync(new ParentNote
        {
            StudentId = 1,
            Title = "  Breakthrough  ",
            Content = "  Finished the chapter alone.  ",
            NoteDate = new DateTime(2026, 5, 4, 16, 0, 0)
        });

        Assert.Equal("Breakthrough", created.Title);
        Assert.Equal("Finished the chapter alone.", created.Content);
        Assert.Equal(new DateTime(2026, 5, 4), created.NoteDate);
        Assert.NotEqual(default, created.CreatedAt);
    }

    [Fact]
    public async Task CreateNoteAsync_RequiresTitle()
    {
        var service = new ParentNoteService(new FakeParentNoteRepository([]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateNoteAsync(new ParentNote { StudentId = 1, Title = "  ", Content = "Body" }));

        Assert.Contains("Title", exception.Message);
    }

    [Fact]
    public async Task CreateNoteAsync_RequiresContent()
    {
        var service = new ParentNoteService(new FakeParentNoteRepository([]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateNoteAsync(new ParentNote { StudentId = 1, Title = "Title", Content = " " }));

        Assert.Contains("content", exception.Message);
    }

    [Fact]
    public async Task UpdateNoteAsync_PreservesOriginalCreatedAt()
    {
        var originalCreatedAt = new DateTime(2026, 1, 15);
        var repository = new FakeParentNoteRepository([
            new ParentNote
            {
                Id = 1,
                StudentId = 1,
                Title = "Original",
                Content = "Original content",
                NoteDate = new DateTime(2026, 1, 15),
                CreatedAt = originalCreatedAt
            }
        ]);
        var service = new ParentNoteService(repository);

        var updated = await service.UpdateNoteAsync(new ParentNote
        {
            Id = 1,
            StudentId = 1,
            Title = "Revised",
            Content = "Revised content",
            NoteDate = new DateTime(2026, 1, 16)
        });

        Assert.Equal(originalCreatedAt, updated.CreatedAt);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateNoteAsync_ThrowsWhenNoteMissing()
    {
        var service = new ParentNoteService(new FakeParentNoteRepository([]));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateNoteAsync(new ParentNote { Id = 7, StudentId = 1, Title = "Ghost", Content = "Gone" }));
    }

    private sealed class FakeParentNoteRepository : IParentNoteRepository
    {
        private readonly List<ParentNote> _notes;

        public FakeParentNoteRepository(IEnumerable<ParentNote> notes)
        {
            _notes = notes.ToList();
        }

        public Task<ParentNote?> GetByIdAsync(int id) => Task.FromResult(_notes.FirstOrDefault(n => n.Id == id));
        public Task<IEnumerable<ParentNote>> GetAllAsync() => Task.FromResult<IEnumerable<ParentNote>>(_notes);

        public Task<ParentNote> AddAsync(ParentNote entity)
        {
            entity.Id = _notes.Select(n => n.Id).DefaultIfEmpty(0).Max() + 1;
            _notes.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(ParentNote entity)
        {
            var index = _notes.FindIndex(n => n.Id == entity.Id);
            if (index >= 0)
            {
                _notes[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _notes.RemoveAll(n => n.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id) => Task.FromResult(_notes.Any(n => n.Id == id));

        public Task<IEnumerable<ParentNote>> GetFilteredAsync(ParentNoteFilter filter) =>
            Task.FromResult<IEnumerable<ParentNote>>(_notes
                .Where(n => !filter.StudentId.HasValue || n.StudentId == filter.StudentId.Value)
                .Where(n => !filter.Category.HasValue || n.Category == filter.Category.Value));

        public Task<IEnumerable<ParentNote>> GetByStudentIdAsync(int studentId) =>
            Task.FromResult<IEnumerable<ParentNote>>(_notes.Where(n => n.StudentId == studentId));
    }
}
