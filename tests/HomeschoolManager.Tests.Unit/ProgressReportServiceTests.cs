using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class ProgressReportServiceTests
{
    [Fact]
    public async Task GenerateAsync_AggregatesReportDataForStudentAndDateRange()
    {
        var student = new Student
        {
            Id = 1,
            FirstName = "Ava",
            LastName = "Stone",
            GradeLevel = "6th",
            DateOfBirth = new DateTime(2014, 1, 1)
        };
        var service = new ProgressReportService(
            new FakeStudentRepository([student]),
            new FakeAssignmentRepository([
                new Assignment { Id = 1, StudentId = 1, Title = "Done In Range", Subject = "Math", DueDate = new DateTime(2026, 5, 3), Status = AssignmentStatus.Completed },
                new Assignment { Id = 2, StudentId = 1, Title = "Done Outside Range", DueDate = new DateTime(2026, 4, 3), Status = AssignmentStatus.Completed },
                new Assignment { Id = 3, StudentId = 1, Title = "Open", DueDate = new DateTime(2026, 5, 4), Status = AssignmentStatus.Assigned },
                new Assignment { Id = 4, StudentId = 2, Title = "Other Student", DueDate = new DateTime(2026, 5, 5), Status = AssignmentStatus.Completed }
            ]),
            new FakeLessonPlanRepository([
                new LessonPlan { Id = 1, StudentId = 1, Title = "Completed Lesson", PlannedDate = new DateTime(2026, 5, 2), Status = LessonPlanStatus.Completed, DurationMinutes = 45 },
                new LessonPlan { Id = 2, StudentId = 1, Title = "Skipped Lesson", PlannedDate = new DateTime(2026, 5, 2), Status = LessonPlanStatus.Skipped },
                new LessonPlan { Id = 3, StudentId = 2, Title = "Other Lesson", PlannedDate = new DateTime(2026, 5, 2), Status = LessonPlanStatus.Completed }
            ]),
            new FakeAttendanceRepository([
                new AttendanceRecord { Id = 1, StudentId = 1, Date = new DateTime(2026, 5, 1), Status = AttendanceStatus.Present, Minutes = 240, Notes = "Strong focus." },
                new AttendanceRecord { Id = 2, StudentId = 1, Date = new DateTime(2026, 5, 2), Status = AttendanceStatus.Absent },
                new AttendanceRecord { Id = 3, StudentId = 2, Date = new DateTime(2026, 5, 1), Status = AttendanceStatus.Present }
            ]),
            new FakeLearningTimeRepository([
                new LearningTimeEntry { Id = 1, StudentId = 1, SubjectId = 1, Subject = "Math", Date = new DateTime(2026, 5, 1), Minutes = 30, Notes = "Fractions review." },
                new LearningTimeEntry { Id = 2, StudentId = 1, SubjectId = 1, Subject = "Math", Date = new DateTime(2026, 5, 2), Minutes = 45 },
                new LearningTimeEntry { Id = 3, StudentId = 1, SubjectId = 2, Subject = "Science", Date = new DateTime(2026, 5, 3), Minutes = 60 },
                new LearningTimeEntry { Id = 4, StudentId = 2, SubjectId = 1, Subject = "Math", Date = new DateTime(2026, 5, 3), Minutes = 999 }
            ]),
            new FakePortfolioRepository([
                new PortfolioItem { Id = 1, StudentId = 1, SubjectId = 1, Title = "Best Work", Date = new DateTime(2026, 5, 4), IsBestWork = true, Notes = "Parent reviewed." },
                new PortfolioItem { Id = 2, StudentId = 1, SubjectId = 1, Title = "Regular Work", Date = new DateTime(2026, 5, 4), IsBestWork = false },
                new PortfolioItem { Id = 3, StudentId = 2, SubjectId = 1, Title = "Other Student", Date = new DateTime(2026, 5, 4), IsBestWork = true }
            ]),
            new FakeParentNoteRepository([
                new ParentNote { Id = 1, StudentId = 1, Category = ParentNoteCategory.Breakthrough, Title = "Reading clicked", Content = "Ava summarized independently.", NoteDate = new DateTime(2026, 5, 5) },
                new ParentNote { Id = 2, StudentId = 1, Category = ParentNoteCategory.Planning, Title = "Outside range", Content = "Old note.", NoteDate = new DateTime(2026, 4, 1) },
                new ParentNote { Id = 3, StudentId = 2, Category = ParentNoteCategory.General, Title = "Other student", Content = "Ignore.", NoteDate = new DateTime(2026, 5, 5) }
            ]));

        var report = await service.GenerateAsync(1, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

        Assert.Equal("Ava", report.Student.FirstName);
        Assert.Equal(1, report.Summary.CompletedAssignmentCount);
        Assert.Equal(1, report.Summary.CompletedLessonCount);
        Assert.Equal(2, report.Summary.AttendanceRecordCount);
        Assert.Equal(1, report.Summary.PresentAttendanceCount);
        Assert.Equal(1, report.Summary.AbsentAttendanceCount);
        Assert.Equal(135, report.Summary.TotalLearningMinutes);
        Assert.Equal(2.25, report.Summary.TotalLearningHours);
        Assert.Equal(1, report.Summary.BestWorkItemCount);
        Assert.Contains(report.LearningTimeBySubject, s => s.Subject == "Math" && s.Minutes == 75);
        Assert.Contains(report.LearningTimeBySubject, s => s.Subject == "Science" && s.Minutes == 60);
        Assert.Equal(4, report.Notes.Count);
        Assert.Contains(report.Notes, n => n.Source == "Parent Note - Breakthrough" && n.Text.Contains("Reading clicked"));
    }

    [Fact]
    public async Task GenerateAsync_UsesUpdatedAtAsCompletionDateWhenAvailable()
    {
        var service = new ProgressReportService(
            new FakeStudentRepository([new Student { Id = 1, FirstName = "Ava", LastName = "Stone", GradeLevel = "6th", DateOfBirth = new DateTime(2014, 1, 1) }]),
            new FakeAssignmentRepository([
                new Assignment { Id = 1, StudentId = 1, Title = "Completed Late", DueDate = new DateTime(2026, 4, 1), UpdatedAt = new DateTime(2026, 5, 5), Status = AssignmentStatus.Completed }
            ]),
            new FakeLessonPlanRepository([
                new LessonPlan { Id = 1, StudentId = 1, Title = "Completed Later", PlannedDate = new DateTime(2026, 4, 1), UpdatedAt = new DateTime(2026, 5, 6), Status = LessonPlanStatus.Completed }
            ]),
            new FakeAttendanceRepository([]),
            new FakeLearningTimeRepository([]),
            new FakePortfolioRepository([]),
            new FakeParentNoteRepository([]));

        var report = await service.GenerateAsync(1, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

        Assert.Single(report.CompletedAssignments);
        Assert.Single(report.CompletedLessons);
    }

    private sealed class FakeStudentRepository : IStudentRepository
    {
        private readonly List<Student> _students;

        public FakeStudentRepository(IEnumerable<Student> students) => _students = students.ToList();
        public Task<Student?> GetByIdAsync(int id) => Task.FromResult(_students.FirstOrDefault(s => s.Id == id));
        public Task<IEnumerable<Student>> GetAllAsync() => Task.FromResult<IEnumerable<Student>>(_students);
        public Task<Student> AddAsync(Student entity) => Task.FromResult(entity);
        public Task UpdateAsync(Student entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_students.Any(s => s.Id == id));
        public Task<IEnumerable<Student>> GetByGradeLevelAsync(string gradeLevel) => Task.FromResult<IEnumerable<Student>>(_students.Where(s => s.GradeLevel == gradeLevel));
        public Task<IEnumerable<Student>> GetActiveStudentsAsync() => Task.FromResult<IEnumerable<Student>>(_students);
        public Task<Student?> GetWithCoursesAsync(int id) => GetByIdAsync(id);
        public Task<Student?> GetWithAssignmentsAsync(int id) => GetByIdAsync(id);
        public Task<Student?> GetWithGradesAsync(int id) => GetByIdAsync(id);
    }

    private sealed class FakeAssignmentRepository : IAssignmentRepository
    {
        private readonly List<Assignment> _assignments;

        public FakeAssignmentRepository(IEnumerable<Assignment> assignments) => _assignments = assignments.ToList();
        public Task<Assignment?> GetByIdAsync(int id) => Task.FromResult(_assignments.FirstOrDefault(a => a.Id == id));
        public Task<IEnumerable<Assignment>> GetAllAsync() => Task.FromResult<IEnumerable<Assignment>>(_assignments);
        public Task<Assignment> AddAsync(Assignment entity) => Task.FromResult(entity);
        public Task UpdateAsync(Assignment entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_assignments.Any(a => a.Id == id));
        public Task<IEnumerable<Assignment>> GetByStudentIdAsync(int studentId) => Task.FromResult<IEnumerable<Assignment>>(_assignments.Where(a => a.StudentId == studentId));
        public Task<IEnumerable<Assignment>> GetOpenAssignmentsAsync() => Task.FromResult<IEnumerable<Assignment>>(_assignments.Where(a => a.Status != AssignmentStatus.Completed));
    }

    private sealed class FakeLessonPlanRepository : ILessonPlanRepository
    {
        private readonly List<LessonPlan> _lessonPlans;

        public FakeLessonPlanRepository(IEnumerable<LessonPlan> lessonPlans) => _lessonPlans = lessonPlans.ToList();
        public Task<LessonPlan?> GetByIdAsync(int id) => Task.FromResult(_lessonPlans.FirstOrDefault(lp => lp.Id == id));
        public Task<IEnumerable<LessonPlan>> GetAllAsync() => Task.FromResult<IEnumerable<LessonPlan>>(_lessonPlans);
        public Task<LessonPlan> AddAsync(LessonPlan entity) => Task.FromResult(entity);
        public Task UpdateAsync(LessonPlan entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_lessonPlans.Any(lp => lp.Id == id));
        public Task<IEnumerable<LessonPlan>> GetByWeekAsync(DateTime weekStart, int? studentId = null, int? subjectId = null) => Task.FromResult<IEnumerable<LessonPlan>>(_lessonPlans);
        public Task<IEnumerable<LessonPlan>> GetByStudentIdAsync(int studentId) => Task.FromResult<IEnumerable<LessonPlan>>(_lessonPlans.Where(lp => lp.StudentId == studentId));
        public Task<IEnumerable<LessonPlan>> GetBySubjectIdAsync(int subjectId) => Task.FromResult<IEnumerable<LessonPlan>>(_lessonPlans.Where(lp => lp.SubjectId == subjectId));
    }

    private sealed class FakeAttendanceRepository : IAttendanceRepository
    {
        private readonly List<AttendanceRecord> _records;

        public FakeAttendanceRepository(IEnumerable<AttendanceRecord> records) => _records = records.ToList();
        public Task<AttendanceRecord?> GetByIdAsync(int id) => Task.FromResult(_records.FirstOrDefault(a => a.Id == id));
        public Task<IEnumerable<AttendanceRecord>> GetAllAsync() => Task.FromResult<IEnumerable<AttendanceRecord>>(_records);
        public Task<AttendanceRecord> AddAsync(AttendanceRecord entity) => Task.FromResult(entity);
        public Task UpdateAsync(AttendanceRecord entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_records.Any(a => a.Id == id));
        public Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateTime date) => Task.FromResult<IEnumerable<AttendanceRecord>>(_records.Where(a => a.Date.Date == date.Date));
        public Task<IEnumerable<AttendanceRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) => Task.FromResult<IEnumerable<AttendanceRecord>>(_records.Where(a => a.Date.Date >= startDate.Date && a.Date.Date <= endDate.Date));
        public Task<IEnumerable<AttendanceRecord>> GetByStudentIdAsync(int studentId) => Task.FromResult<IEnumerable<AttendanceRecord>>(_records.Where(a => a.StudentId == studentId));
        public Task<AttendanceRecord?> GetByStudentAndDateAsync(int studentId, DateTime date) => Task.FromResult(_records.FirstOrDefault(a => a.StudentId == studentId && a.Date.Date == date.Date));
    }

    private sealed class FakeLearningTimeRepository : ILearningTimeRepository
    {
        private readonly List<LearningTimeEntry> _entries;

        public FakeLearningTimeRepository(IEnumerable<LearningTimeEntry> entries) => _entries = entries.ToList();
        public Task<LearningTimeEntry?> GetByIdAsync(int id) => Task.FromResult(_entries.FirstOrDefault(e => e.Id == id));
        public Task<IEnumerable<LearningTimeEntry>> GetAllAsync() => Task.FromResult<IEnumerable<LearningTimeEntry>>(_entries);
        public Task<LearningTimeEntry> AddAsync(LearningTimeEntry entity) => Task.FromResult(entity);
        public Task UpdateAsync(LearningTimeEntry entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_entries.Any(e => e.Id == id));
        public Task<IEnumerable<LearningTimeEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) => Task.FromResult<IEnumerable<LearningTimeEntry>>(_entries.Where(e => e.Date.Date >= startDate.Date && e.Date.Date <= endDate.Date));
        public Task<IEnumerable<LearningTimeEntry>> GetByStudentIdAsync(int studentId) => Task.FromResult<IEnumerable<LearningTimeEntry>>(_entries.Where(e => e.StudentId == studentId));
        public Task<LearningTimeEntry?> GetBySourceAsync(LearningTimeSource source, int sourceId) => Task.FromResult(_entries.FirstOrDefault(e => e.Source == source && e.SourceId == sourceId));
    }

    private sealed class FakePortfolioRepository : IPortfolioRepository
    {
        private readonly List<PortfolioItem> _items;

        public FakePortfolioRepository(IEnumerable<PortfolioItem> items) => _items = items.ToList();
        public Task<PortfolioItem?> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault(i => i.Id == id));
        public Task<IEnumerable<PortfolioItem>> GetAllAsync() => Task.FromResult<IEnumerable<PortfolioItem>>(_items);
        public Task<PortfolioItem> AddAsync(PortfolioItem entity) => Task.FromResult(entity);
        public Task UpdateAsync(PortfolioItem entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_items.Any(i => i.Id == id));
        public Task<IEnumerable<PortfolioItem>> GetFilteredAsync(PortfolioFilter filter)
        {
            var query = _items.AsEnumerable();
            if (filter.StudentId.HasValue)
            {
                query = query.Where(i => i.StudentId == filter.StudentId.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(i => i.Date.Date >= filter.StartDate.Value.Date);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(i => i.Date.Date <= filter.EndDate.Value.Date);
            }

            if (filter.BestWorkOnly)
            {
                query = query.Where(i => i.IsBestWork);
            }

            return Task.FromResult<IEnumerable<PortfolioItem>>(query);
        }

        public Task<IEnumerable<PortfolioItem>> GetByStudentIdAsync(int studentId) => Task.FromResult<IEnumerable<PortfolioItem>>(_items.Where(i => i.StudentId == studentId));
        public Task<IEnumerable<PortfolioItem>> GetByAssignmentIdAsync(int assignmentId) => Task.FromResult<IEnumerable<PortfolioItem>>(_items.Where(i => i.AssignmentId == assignmentId));
        public Task<IEnumerable<PortfolioItem>> GetByLessonPlanIdAsync(int lessonPlanId) => Task.FromResult<IEnumerable<PortfolioItem>>(_items.Where(i => i.LessonPlanId == lessonPlanId));
    }

    private sealed class FakeParentNoteRepository : IParentNoteRepository
    {
        private readonly List<ParentNote> _notes;

        public FakeParentNoteRepository(IEnumerable<ParentNote> notes) => _notes = notes.ToList();
        public Task<ParentNote?> GetByIdAsync(int id) => Task.FromResult(_notes.FirstOrDefault(n => n.Id == id));
        public Task<IEnumerable<ParentNote>> GetAllAsync() => Task.FromResult<IEnumerable<ParentNote>>(_notes);
        public Task<ParentNote> AddAsync(ParentNote entity) => Task.FromResult(entity);
        public Task UpdateAsync(ParentNote entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_notes.Any(n => n.Id == id));
        public Task<IEnumerable<ParentNote>> GetByStudentIdAsync(int studentId) => Task.FromResult<IEnumerable<ParentNote>>(_notes.Where(n => n.StudentId == studentId));

        public Task<IEnumerable<ParentNote>> GetFilteredAsync(ParentNoteFilter filter)
        {
            var query = _notes.AsEnumerable();
            if (filter.StudentId.HasValue)
            {
                query = query.Where(n => n.StudentId == filter.StudentId.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(n => n.NoteDate.Date >= filter.StartDate.Value.Date);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(n => n.NoteDate.Date <= filter.EndDate.Value.Date);
            }

            return Task.FromResult<IEnumerable<ParentNote>>(query);
        }
    }
}
