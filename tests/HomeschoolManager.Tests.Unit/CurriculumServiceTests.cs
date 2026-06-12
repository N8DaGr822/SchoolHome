using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class CurriculumServiceTests
{
    [Fact]
    public async Task CreateResourceAsync_RequiresTitle()
    {
        var service = CreateService(out _, out _);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateResourceAsync(new CurriculumResource { Title = "  ", SubjectId = 1 }));

        Assert.Contains("Title", exception.Message);
    }

    [Fact]
    public async Task AssignResourceAsync_AssignsDistinctStudentsAndSkipsExisting()
    {
        var service = CreateService(out var resources, out var studentCurricula);
        resources.Seed(new CurriculumResource { Id = 1, Title = "Math Book", SubjectId = 1 });
        studentCurricula.Seed(new StudentCurriculum { Id = 1, StudentId = 1, CurriculumResourceId = 1 });

        var assigned = (await service.AssignResourceAsync(
            1,
            [1, 2, 2, 0],
            startDate: new DateTime(2026, 8, 1),
            targetEndDate: new DateTime(2027, 5, 31))).ToList();

        Assert.Equal(2, assigned.Count);
        // Student 1 keeps the existing assignment; student 2 gets a new one.
        Assert.Contains(assigned, c => c.StudentId == 1 && c.Id == 1);
        Assert.Contains(assigned, c => c.StudentId == 2 && c.Status == CurriculumStatus.NotStarted);
    }

    [Fact]
    public async Task AssignResourceAsync_ThrowsWhenResourceMissing()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignResourceAsync(99, [1], null, null));
    }

    [Fact]
    public async Task AssignResourceAsync_ThrowsWhenNoStudentsSelected()
    {
        var service = CreateService(out var resources, out _);
        resources.Seed(new CurriculumResource { Id = 1, Title = "Science Kit", SubjectId = 1 });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignResourceAsync(1, [0, -3], null, null));

        Assert.Contains("at least one student", exception.Message);
    }

    [Fact]
    public async Task UpdateStudentCurriculumAsync_PreservesStudentAndResourceLinks()
    {
        var service = CreateService(out _, out var studentCurricula);
        studentCurricula.Seed(new StudentCurriculum
        {
            Id = 1,
            StudentId = 1,
            CurriculumResourceId = 5,
            CreatedAt = new DateTime(2026, 1, 1)
        });

        var updated = await service.UpdateStudentCurriculumAsync(new StudentCurriculum
        {
            Id = 1,
            StudentId = 99,
            CurriculumResourceId = 42,
            PercentComplete = 50
        });

        Assert.Equal(1, updated.StudentId);
        Assert.Equal(5, updated.CurriculumResourceId);
        Assert.Equal(new DateTime(2026, 1, 1), updated.CreatedAt);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStudentCurriculumAsync_RejectsOutOfRangePercentComplete()
    {
        var service = CreateService(out _, out var studentCurricula);
        studentCurricula.Seed(new StudentCurriculum { Id = 1, StudentId = 1, CurriculumResourceId = 1 });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStudentCurriculumAsync(new StudentCurriculum
            {
                Id = 1,
                StudentId = 1,
                CurriculumResourceId = 1,
                PercentComplete = 150
            }));

        Assert.Contains("between 0 and 100", exception.Message);
    }

    private static CurriculumService CreateService(
        out FakeCurriculumResourceRepository resources,
        out FakeStudentCurriculumRepository studentCurricula)
    {
        resources = new FakeCurriculumResourceRepository();
        studentCurricula = new FakeStudentCurriculumRepository();
        return new CurriculumService(resources, studentCurricula);
    }

    private sealed class FakeCurriculumResourceRepository : ICurriculumResourceRepository
    {
        private readonly List<CurriculumResource> _resources = [];

        public void Seed(CurriculumResource resource) => _resources.Add(resource);

        public Task<CurriculumResource?> GetByIdAsync(int id) => Task.FromResult(_resources.FirstOrDefault(r => r.Id == id));
        public Task<IEnumerable<CurriculumResource>> GetAllAsync() => Task.FromResult<IEnumerable<CurriculumResource>>(_resources);

        public Task<CurriculumResource> AddAsync(CurriculumResource entity)
        {
            entity.Id = _resources.Select(r => r.Id).DefaultIfEmpty(0).Max() + 1;
            _resources.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(CurriculumResource entity)
        {
            var index = _resources.FindIndex(r => r.Id == entity.Id);
            if (index >= 0)
            {
                _resources[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _resources.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id) => Task.FromResult(_resources.Any(r => r.Id == id));

        public Task<IEnumerable<CurriculumResource>> GetFilteredAsync(CurriculumResourceFilter filter) =>
            Task.FromResult<IEnumerable<CurriculumResource>>(_resources
                .Where(r => !filter.SubjectId.HasValue || r.SubjectId == filter.SubjectId.Value)
                .Where(r => !filter.ResourceType.HasValue || r.ResourceType == filter.ResourceType.Value));
    }

    private sealed class FakeStudentCurriculumRepository : IStudentCurriculumRepository
    {
        private readonly List<StudentCurriculum> _items = [];

        public void Seed(StudentCurriculum item) => _items.Add(item);

        public Task<StudentCurriculum?> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault(c => c.Id == id));
        public Task<IEnumerable<StudentCurriculum>> GetAllAsync() => Task.FromResult<IEnumerable<StudentCurriculum>>(_items);

        public Task<StudentCurriculum> AddAsync(StudentCurriculum entity)
        {
            entity.Id = _items.Select(c => c.Id).DefaultIfEmpty(0).Max() + 1;
            _items.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(StudentCurriculum entity)
        {
            var index = _items.FindIndex(c => c.Id == entity.Id);
            if (index >= 0)
            {
                _items[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _items.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id) => Task.FromResult(_items.Any(c => c.Id == id));

        public Task<IEnumerable<StudentCurriculum>> GetFilteredAsync(StudentCurriculumFilter filter) =>
            Task.FromResult<IEnumerable<StudentCurriculum>>(_items
                .Where(c => !filter.StudentId.HasValue || c.StudentId == filter.StudentId.Value)
                .Where(c => !filter.Status.HasValue || c.Status == filter.Status.Value));

        public Task<IEnumerable<StudentCurriculum>> GetByStudentIdAsync(int studentId) =>
            Task.FromResult<IEnumerable<StudentCurriculum>>(_items.Where(c => c.StudentId == studentId));

        public Task<IEnumerable<StudentCurriculum>> GetByResourceIdAsync(int resourceId) =>
            Task.FromResult<IEnumerable<StudentCurriculum>>(_items.Where(c => c.CurriculumResourceId == resourceId));

        public Task<StudentCurriculum?> GetByStudentAndResourceAsync(int studentId, int resourceId) =>
            Task.FromResult(_items.FirstOrDefault(c => c.StudentId == studentId && c.CurriculumResourceId == resourceId));
    }
}
