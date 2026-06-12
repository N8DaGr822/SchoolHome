using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class AssignmentServiceTests
{
    [Fact]
    public async Task CreateAssignmentAsync_StampsAssignedDateAndCreatedAt()
    {
        var repository = new FakeAssignmentRepository([]);
        var service = new AssignmentService(repository);

        var created = await service.CreateAssignmentAsync(new Assignment
        {
            StudentId = 1,
            CourseId = 1,
            Title = "Fractions Worksheet",
            DueDate = DateTime.Today.AddDays(3)
        });

        Assert.NotEqual(default, created.AssignedDate);
        Assert.NotEqual(default, created.CreatedAt);
    }

    [Fact]
    public async Task CreateAssignmentAsync_PreservesExplicitAssignedDate()
    {
        var assignedDate = new DateTime(2026, 5, 1);
        var repository = new FakeAssignmentRepository([]);
        var service = new AssignmentService(repository);

        var created = await service.CreateAssignmentAsync(new Assignment
        {
            StudentId = 1,
            CourseId = 1,
            Title = "Essay Draft",
            AssignedDate = assignedDate,
            DueDate = assignedDate.AddDays(7)
        });

        Assert.Equal(assignedDate, created.AssignedDate);
    }

    [Fact]
    public async Task CompleteAssignmentAsync_MarksCompletedAndStampsUpdatedAt()
    {
        var repository = new FakeAssignmentRepository([
            new Assignment { Id = 1, StudentId = 1, CourseId = 1, Title = "Lab Report", Status = AssignmentStatus.InProgress }
        ]);
        var service = new AssignmentService(repository);

        var completed = await service.CompleteAssignmentAsync(1);

        Assert.Equal(AssignmentStatus.Completed, completed.Status);
        Assert.NotNull(completed.UpdatedAt);
        Assert.Equal(AssignmentStatus.Completed, (await repository.GetByIdAsync(1))!.Status);
    }

    [Fact]
    public async Task CompleteAssignmentAsync_ThrowsWhenAssignmentMissing()
    {
        var service = new AssignmentService(new FakeAssignmentRepository([]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAssignmentAsync(99));

        Assert.Contains("99", exception.Message);
    }

    [Fact]
    public async Task CompleteAssignmentAsync_CreatesLearningTimeEntryWhenRequested()
    {
        var repository = new FakeAssignmentRepository([
            new Assignment { Id = 1, StudentId = 1, CourseId = 1, Title = "Reading", Status = AssignmentStatus.Assigned }
        ]);
        var learningTime = new RecordingLearningTimeService();
        var service = new AssignmentService(repository, learningTime);

        await service.CompleteAssignmentAsync(1, createLearningTimeEntry: true);

        Assert.NotNull(learningTime.CompletedAssignment);
        Assert.Equal(1, learningTime.CompletedAssignment!.Id);
    }

    [Fact]
    public async Task CompleteAssignmentAsync_SkipsLearningTimeEntryWhenNotRequested()
    {
        var repository = new FakeAssignmentRepository([
            new Assignment { Id = 1, StudentId = 1, CourseId = 1, Title = "Reading", Status = AssignmentStatus.Assigned }
        ]);
        var learningTime = new RecordingLearningTimeService();
        var service = new AssignmentService(repository, learningTime);

        await service.CompleteAssignmentAsync(1, createLearningTimeEntry: false);

        Assert.Null(learningTime.CompletedAssignment);
    }

    private sealed class FakeAssignmentRepository : IAssignmentRepository
    {
        private readonly List<Assignment> _assignments;

        public FakeAssignmentRepository(IEnumerable<Assignment> assignments)
        {
            _assignments = assignments.ToList();
        }

        public Task<Assignment?> GetByIdAsync(int id) => Task.FromResult(_assignments.FirstOrDefault(a => a.Id == id));
        public Task<IEnumerable<Assignment>> GetAllAsync() => Task.FromResult<IEnumerable<Assignment>>(_assignments);

        public Task<Assignment> AddAsync(Assignment entity)
        {
            entity.Id = _assignments.Select(a => a.Id).DefaultIfEmpty(0).Max() + 1;
            _assignments.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(Assignment entity)
        {
            var index = _assignments.FindIndex(a => a.Id == entity.Id);
            if (index >= 0)
            {
                _assignments[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _assignments.RemoveAll(a => a.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id) => Task.FromResult(_assignments.Any(a => a.Id == id));

        public Task<IEnumerable<Assignment>> GetByStudentIdAsync(int studentId) =>
            Task.FromResult<IEnumerable<Assignment>>(_assignments.Where(a => a.StudentId == studentId));

        public Task<IEnumerable<Assignment>> GetOpenAssignmentsAsync() =>
            Task.FromResult<IEnumerable<Assignment>>(_assignments.Where(a =>
                a.Status is AssignmentStatus.Assigned or AssignmentStatus.InProgress or AssignmentStatus.Overdue));
    }

    private sealed class RecordingLearningTimeService : ILearningTimeService
    {
        public Assignment? CompletedAssignment { get; private set; }

        public Task<LearningTimeEntry?> GetEntryByIdAsync(int id) => Task.FromResult<LearningTimeEntry?>(null);
        public Task<IEnumerable<LearningTimeEntry>> GetEntriesAsync(DateTime startDate, DateTime endDate) =>
            Task.FromResult<IEnumerable<LearningTimeEntry>>([]);
        public Task<IEnumerable<LearningTimeEntry>> GetEntriesForStudentAsync(int studentId) =>
            Task.FromResult<IEnumerable<LearningTimeEntry>>([]);
        public Task<LearningTimeEntry> CreateEntryAsync(LearningTimeEntry entry) => Task.FromResult(entry);
        public Task<LearningTimeEntry> UpdateEntryAsync(LearningTimeEntry entry) => Task.FromResult(entry);
        public Task DeleteEntryAsync(int id) => Task.CompletedTask;
        public Task<LearningTimeReport> GetReportAsync(DateTime startDate, DateTime endDate) =>
            throw new NotSupportedException();

        public Task<LearningTimeEntry?> CreateFromAssignmentCompletionAsync(Assignment assignment)
        {
            CompletedAssignment = assignment;
            return Task.FromResult<LearningTimeEntry?>(null);
        }

        public Task<LearningTimeEntry?> CreateFromLessonCompletionAsync(LessonPlan lessonPlan) =>
            Task.FromResult<LearningTimeEntry?>(null);
    }
}
