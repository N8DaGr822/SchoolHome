using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class CourseServiceTests
{
    [Fact]
    public async Task CreateCourseAsync_StampsCreatedAt()
    {
        var repository = new FakeCourseRepository([]);
        var service = new CourseService(repository);

        var created = await service.CreateCourseAsync(new Course { Name = "Algebra", Subject = "Math" });

        Assert.NotEqual(default, created.CreatedAt);
    }

    [Fact]
    public async Task AddLessonPlanAsync_AssignsSequentialIdAndCourseId()
    {
        var repository = new FakeCourseRepository([
            new Course
            {
                Id = 1,
                Name = "Biology",
                LessonPlans = [new LessonPlan { Id = 3, Title = "Cells" }]
            }
        ]);
        var service = new CourseService(repository);

        var added = await service.AddLessonPlanAsync(1, new LessonPlan { Title = "Photosynthesis" });

        Assert.Equal(4, added.Id);
        Assert.Equal(1, added.CourseId);
        Assert.NotEqual(default, added.CreatedAt);

        var course = await repository.GetByIdAsync(1);
        Assert.Equal(2, course!.LessonPlans.Count);
    }

    [Fact]
    public async Task AddLessonPlanAsync_ThrowsWhenCourseMissing()
    {
        var service = new CourseService(new FakeCourseRepository([]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddLessonPlanAsync(42, new LessonPlan { Title = "Orphan" }));

        Assert.Contains("42", exception.Message);
    }

    [Fact]
    public async Task UpdateLessonPlanAsync_ReplacesExistingLessonPlan()
    {
        var repository = new FakeCourseRepository([
            new Course
            {
                Id = 1,
                Name = "History",
                LessonPlans = [new LessonPlan { Id = 1, Title = "Ancient Rome" }]
            }
        ]);
        var service = new CourseService(repository);

        var updated = await service.UpdateLessonPlanAsync(1, new LessonPlan { Id = 1, Title = "Ancient Greece" });

        Assert.Equal("Ancient Greece", updated.Title);
        Assert.NotNull(updated.UpdatedAt);

        var course = await repository.GetByIdAsync(1);
        Assert.Equal("Ancient Greece", course!.LessonPlans.Single().Title);
    }

    [Fact]
    public async Task UpdateLessonPlanAsync_ThrowsWhenLessonPlanMissing()
    {
        var repository = new FakeCourseRepository([
            new Course { Id = 1, Name = "History", LessonPlans = [] }
        ]);
        var service = new CourseService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateLessonPlanAsync(1, new LessonPlan { Id = 9, Title = "Missing" }));
    }

    [Fact]
    public async Task DeleteLessonPlanAsync_RemovesLessonPlanFromCourse()
    {
        var repository = new FakeCourseRepository([
            new Course
            {
                Id = 1,
                Name = "Art",
                LessonPlans = [new LessonPlan { Id = 1, Title = "Watercolors" }]
            }
        ]);
        var service = new CourseService(repository);

        await service.DeleteLessonPlanAsync(1, 1);

        var course = await repository.GetByIdAsync(1);
        Assert.Empty(course!.LessonPlans);
    }

    private sealed class FakeCourseRepository : IRepository<Course>
    {
        private readonly List<Course> _courses;

        public FakeCourseRepository(IEnumerable<Course> courses)
        {
            _courses = courses.ToList();
        }

        public Task<Course?> GetByIdAsync(int id) => Task.FromResult(_courses.FirstOrDefault(c => c.Id == id));
        public Task<IEnumerable<Course>> GetAllAsync() => Task.FromResult<IEnumerable<Course>>(_courses);

        public Task<Course> AddAsync(Course entity)
        {
            entity.Id = _courses.Select(c => c.Id).DefaultIfEmpty(0).Max() + 1;
            _courses.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(Course entity)
        {
            var index = _courses.FindIndex(c => c.Id == entity.Id);
            if (index >= 0)
            {
                _courses[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _courses.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id) => Task.FromResult(_courses.Any(c => c.Id == id));
    }
}
