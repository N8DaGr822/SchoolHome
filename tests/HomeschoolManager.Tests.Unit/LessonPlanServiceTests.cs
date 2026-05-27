using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class LessonPlanServiceTests
{
    [Fact]
    public async Task CompleteLessonPlanAsync_MarksLessonCompleted()
    {
        var fixture = CreateFixture();
        var lesson = await fixture.Service.CreateLessonPlanAsync(CreateLesson());

        var completed = await fixture.Service.CompleteLessonPlanAsync(lesson.Id);

        Assert.Equal(LessonPlanStatus.Completed, completed.Status);
        Assert.Equal(LessonPlanStatus.Completed, (await fixture.LessonPlans.GetByIdAsync(lesson.Id))?.Status);
    }

    [Fact]
    public async Task MoveLessonPlanAsync_ChangesPlannedDate()
    {
        var fixture = CreateFixture();
        var lesson = await fixture.Service.CreateLessonPlanAsync(CreateLesson());
        var movedDate = lesson.PlannedDate.AddDays(2);

        var moved = await fixture.Service.MoveLessonPlanAsync(lesson.Id, movedDate);

        Assert.Equal(movedDate.Date, moved.PlannedDate);
    }

    [Fact]
    public async Task ConvertToAssignmentAsync_CreatesAssignmentAndLinksLesson()
    {
        var fixture = CreateFixture();
        var lesson = await fixture.Service.CreateLessonPlanAsync(CreateLesson());

        var assignment = await fixture.Service.ConvertToAssignmentAsync(lesson.Id);
        var linkedLesson = await fixture.LessonPlans.GetByIdAsync(lesson.Id);

        Assert.Equal("Fractions", assignment.Title);
        Assert.Equal(lesson.StudentId, assignment.StudentId);
        Assert.Equal(lesson.SubjectId, assignment.CourseId);
        Assert.Equal(assignment.Id, linkedLesson?.AssignmentId);
    }

    private static LessonPlan CreateLesson()
    {
        return new LessonPlan
        {
            FamilyId = 1,
            StudentId = 1,
            SubjectId = 1,
            Title = "Fractions",
            Description = "Practice equivalent fractions.",
            PlannedDate = new DateTime(2026, 5, 27),
            EstimatedMinutes = 30
        };
    }

    private static Fixture CreateFixture()
    {
        var lessonPlans = new FakeLessonPlanRepository();
        var students = new FakeStudentRepository();
        var courses = new FakeCourseRepository();
        var assignments = new FakeAssignmentRepository();
        var service = new LessonPlanService(lessonPlans, students, courses, assignments);
        return new Fixture(service, lessonPlans);
    }

    private sealed record Fixture(LessonPlanService Service, FakeLessonPlanRepository LessonPlans);

    private sealed class FakeLessonPlanRepository : ILessonPlanRepository
    {
        private readonly List<LessonPlan> _lessonPlans = new();

        public Task<LessonPlan?> GetByIdAsync(int id)
        {
            return Task.FromResult(_lessonPlans.FirstOrDefault(lp => lp.Id == id));
        }

        public Task<IEnumerable<LessonPlan>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<LessonPlan>>(_lessonPlans);
        }

        public Task<LessonPlan> AddAsync(LessonPlan entity)
        {
            entity.Id = _lessonPlans.Select(lp => lp.Id).DefaultIfEmpty(0).Max() + 1;
            _lessonPlans.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(LessonPlan entity)
        {
            var index = _lessonPlans.FindIndex(lp => lp.Id == entity.Id);
            if (index >= 0)
            {
                _lessonPlans[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _lessonPlans.RemoveAll(lp => lp.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id)
        {
            return Task.FromResult(_lessonPlans.Any(lp => lp.Id == id));
        }

        public Task<IEnumerable<LessonPlan>> GetByWeekAsync(DateTime weekStart, int? studentId = null, int? subjectId = null)
        {
            return Task.FromResult<IEnumerable<LessonPlan>>(_lessonPlans
                .Where(lp => lp.PlannedDate.Date >= weekStart.Date && lp.PlannedDate.Date < weekStart.Date.AddDays(7))
                .Where(lp => !studentId.HasValue || lp.StudentId == studentId.Value)
                .Where(lp => !subjectId.HasValue || lp.SubjectId == subjectId.Value));
        }

        public Task<IEnumerable<LessonPlan>> GetByStudentIdAsync(int studentId)
        {
            return Task.FromResult<IEnumerable<LessonPlan>>(_lessonPlans.Where(lp => lp.StudentId == studentId));
        }

        public Task<IEnumerable<LessonPlan>> GetBySubjectIdAsync(int subjectId)
        {
            return Task.FromResult<IEnumerable<LessonPlan>>(_lessonPlans.Where(lp => lp.SubjectId == subjectId));
        }
    }

    private sealed class FakeStudentRepository : IStudentRepository
    {
        public Task<Student?> GetByIdAsync(int id) => Task.FromResult(id == 1 ? new Student { Id = 1 } : null);
        public Task<IEnumerable<Student>> GetAllAsync() => Task.FromResult<IEnumerable<Student>>([new Student { Id = 1 }]);
        public Task<Student> AddAsync(Student entity) => Task.FromResult(entity);
        public Task UpdateAsync(Student entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(id == 1);
        public Task<IEnumerable<Student>> GetByGradeLevelAsync(string gradeLevel) => GetAllAsync();
        public Task<IEnumerable<Student>> GetActiveStudentsAsync() => GetAllAsync();
        public Task<Student?> GetWithCoursesAsync(int id) => GetByIdAsync(id);
        public Task<Student?> GetWithAssignmentsAsync(int id) => GetByIdAsync(id);
        public Task<Student?> GetWithGradesAsync(int id) => GetByIdAsync(id);
    }

    private sealed class FakeCourseRepository : IRepository<Course>
    {
        public Task<Course?> GetByIdAsync(int id)
        {
            return Task.FromResult(id == 1 ? new Course { Id = 1, Name = "Math", Subject = "Math" } : null);
        }

        public Task<IEnumerable<Course>> GetAllAsync() => Task.FromResult<IEnumerable<Course>>([new Course { Id = 1 }]);
        public Task<Course> AddAsync(Course entity) => Task.FromResult(entity);
        public Task UpdateAsync(Course entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(id == 1);
    }

    private sealed class FakeAssignmentRepository : IAssignmentRepository
    {
        private readonly List<Assignment> _assignments = new();

        public Task<Assignment?> GetByIdAsync(int id) => Task.FromResult(_assignments.FirstOrDefault(a => a.Id == id));
        public Task<IEnumerable<Assignment>> GetAllAsync() => Task.FromResult<IEnumerable<Assignment>>(_assignments);

        public Task<Assignment> AddAsync(Assignment entity)
        {
            entity.Id = _assignments.Select(a => a.Id).DefaultIfEmpty(0).Max() + 1;
            _assignments.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(Assignment entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_assignments.Any(a => a.Id == id));
        public Task<IEnumerable<Assignment>> GetByStudentIdAsync(int studentId) => Task.FromResult<IEnumerable<Assignment>>(_assignments.Where(a => a.StudentId == studentId));
        public Task<IEnumerable<Assignment>> GetOpenAssignmentsAsync() => Task.FromResult<IEnumerable<Assignment>>(_assignments.Where(a => a.Status != AssignmentStatus.Completed));
    }
}
