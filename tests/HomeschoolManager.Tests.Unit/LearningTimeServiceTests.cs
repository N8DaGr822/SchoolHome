using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class LearningTimeServiceTests
{
    [Fact]
    public async Task GetReportAsync_AggregatesMinutesByStudentSubjectAndDateRange()
    {
        var students = new FakeStudentRepository([
            new Student { Id = 1, FirstName = "Ava", LastName = "Brown" },
            new Student { Id = 2, FirstName = "Noah", LastName = "Green" }
        ]);
        var courses = new FakeCourseRepository([
            new Course { Id = 1, Name = "Algebra", Subject = "Math" },
            new Course { Id = 2, Name = "Biology", Subject = "Science" }
        ]);
        var entries = new FakeLearningTimeRepository([
            new LearningTimeEntry { Id = 1, StudentId = 1, SubjectId = 1, Subject = "Math", Date = new DateTime(2026, 5, 1), Minutes = 30 },
            new LearningTimeEntry { Id = 2, StudentId = 1, SubjectId = 2, Subject = "Science", Date = new DateTime(2026, 5, 2), Minutes = 45 },
            new LearningTimeEntry { Id = 3, StudentId = 2, SubjectId = 1, Subject = "Math", Date = new DateTime(2026, 5, 3), Minutes = 60 },
            new LearningTimeEntry { Id = 4, StudentId = 2, SubjectId = 1, Subject = "Math", Date = new DateTime(2026, 4, 30), Minutes = 999 }
        ]);
        var service = new LearningTimeService(entries, students, courses);

        var report = await service.GetReportAsync(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

        Assert.Equal(135, report.TotalMinutes);
        Assert.Equal(75, report.ByStudent.Single(row => row.StudentId == 1).Minutes);
        Assert.Equal(60, report.ByStudent.Single(row => row.StudentId == 2).Minutes);
        Assert.Equal(90, report.BySubject.Single(row => row.SubjectId == 1).Minutes);
        Assert.Equal(45, report.BySubject.Single(row => row.SubjectId == 2).Minutes);
        Assert.DoesNotContain(report.ByDate, row => row.Date == new DateTime(2026, 4, 30));
    }

    [Fact]
    public async Task CreateEntryAsync_RejectsNonPositiveMinutes()
    {
        var service = new LearningTimeService(
            new FakeLearningTimeRepository([]),
            new FakeStudentRepository([new Student { Id = 1 }]),
            new FakeCourseRepository([new Course { Id = 1 }]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateEntryAsync(new LearningTimeEntry
            {
                StudentId = 1,
                SubjectId = 1,
                Date = DateTime.Today,
                Minutes = 0
            }));

        Assert.Contains("positive", exception.Message);
    }

    private sealed class FakeLearningTimeRepository : ILearningTimeRepository
    {
        private readonly List<LearningTimeEntry> _entries;

        public FakeLearningTimeRepository(IEnumerable<LearningTimeEntry> entries)
        {
            _entries = entries.ToList();
        }

        public Task<LearningTimeEntry?> GetByIdAsync(int id) => Task.FromResult(_entries.FirstOrDefault(e => e.Id == id));
        public Task<IEnumerable<LearningTimeEntry>> GetAllAsync() => Task.FromResult<IEnumerable<LearningTimeEntry>>(_entries);

        public Task<LearningTimeEntry> AddAsync(LearningTimeEntry entity)
        {
            entity.Id = _entries.Select(e => e.Id).DefaultIfEmpty(0).Max() + 1;
            _entries.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(LearningTimeEntry entity)
        {
            var index = _entries.FindIndex(e => e.Id == entity.Id);
            if (index >= 0)
            {
                _entries[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _entries.RemoveAll(e => e.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id) => Task.FromResult(_entries.Any(e => e.Id == id));

        public Task<IEnumerable<LearningTimeEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return Task.FromResult<IEnumerable<LearningTimeEntry>>(_entries
                .Where(e => e.Date.Date >= startDate.Date && e.Date.Date <= endDate.Date));
        }

        public Task<IEnumerable<LearningTimeEntry>> GetByStudentIdAsync(int studentId)
        {
            return Task.FromResult<IEnumerable<LearningTimeEntry>>(_entries.Where(e => e.StudentId == studentId));
        }

        public Task<LearningTimeEntry?> GetBySourceAsync(LearningTimeSource source, int sourceId)
        {
            return Task.FromResult(_entries.FirstOrDefault(e => e.Source == source && e.SourceId == sourceId));
        }
    }

    private sealed class FakeStudentRepository : IStudentRepository
    {
        private readonly List<Student> _students;

        public FakeStudentRepository(IEnumerable<Student> students)
        {
            _students = students.ToList();
        }

        public Task<Student?> GetByIdAsync(int id) => Task.FromResult(_students.FirstOrDefault(s => s.Id == id));
        public Task<IEnumerable<Student>> GetAllAsync() => Task.FromResult<IEnumerable<Student>>(_students);
        public Task<Student> AddAsync(Student entity) => Task.FromResult(entity);
        public Task UpdateAsync(Student entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_students.Any(s => s.Id == id));
        public Task<IEnumerable<Student>> GetByGradeLevelAsync(string gradeLevel) => GetAllAsync();
        public Task<IEnumerable<Student>> GetActiveStudentsAsync() => GetAllAsync();
        public Task<Student?> GetWithCoursesAsync(int id) => GetByIdAsync(id);
        public Task<Student?> GetWithAssignmentsAsync(int id) => GetByIdAsync(id);
        public Task<Student?> GetWithGradesAsync(int id) => GetByIdAsync(id);
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
        public Task<Course> AddAsync(Course entity) => Task.FromResult(entity);
        public Task UpdateAsync(Course entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_courses.Any(c => c.Id == id));
    }
}
