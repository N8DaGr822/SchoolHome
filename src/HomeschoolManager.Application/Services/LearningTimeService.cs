using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class LearningTimeService : ILearningTimeService
{
    private readonly ILearningTimeRepository _learningTimeRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IRepository<Course> _courseRepository;

    public LearningTimeService(
        ILearningTimeRepository learningTimeRepository,
        IStudentRepository studentRepository,
        IRepository<Course> courseRepository)
    {
        _learningTimeRepository = learningTimeRepository;
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
    }

    public async Task<LearningTimeEntry?> GetEntryByIdAsync(int id)
    {
        return await _learningTimeRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<LearningTimeEntry>> GetEntriesAsync(DateTime startDate, DateTime endDate)
    {
        return await _learningTimeRepository.GetByDateRangeAsync(startDate, endDate);
    }

    public async Task<IEnumerable<LearningTimeEntry>> GetEntriesForStudentAsync(int studentId)
    {
        return await _learningTimeRepository.GetByStudentIdAsync(studentId);
    }

    public async Task<LearningTimeEntry> CreateEntryAsync(LearningTimeEntry entry)
    {
        await ValidateEntryAsync(entry);
        Normalize(entry);
        entry.CreatedAt = DateTime.UtcNow;
        return await _learningTimeRepository.AddAsync(entry);
    }

    public async Task<LearningTimeEntry> UpdateEntryAsync(LearningTimeEntry entry)
    {
        await ValidateEntryAsync(entry);
        Normalize(entry);
        entry.UpdatedAt = DateTime.UtcNow;
        await _learningTimeRepository.UpdateAsync(entry);
        return entry;
    }

    public async Task DeleteEntryAsync(int id)
    {
        await _learningTimeRepository.DeleteAsync(id);
    }

    public async Task<LearningTimeReport> GetReportAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        if (end < start)
        {
            (start, end) = (end, start);
        }

        var entries = (await _learningTimeRepository.GetByDateRangeAsync(start, end)).ToList();
        var students = (await _studentRepository.GetAllAsync())
            .ToDictionary(student => student.Id, student => $"{student.FirstName} {student.LastName}");
        var courses = (await _courseRepository.GetAllAsync())
            .ToDictionary(course => course.Id, course => string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject);

        return new LearningTimeReport(
            start,
            end,
            entries.Sum(entry => entry.Minutes),
            entries
                .GroupBy(entry => entry.StudentId)
                .Select(group => new LearningTimeStudentTotal(
                    group.Key,
                    students.TryGetValue(group.Key, out var studentName) ? studentName : "Unknown student",
                    group.Sum(entry => entry.Minutes)))
                .OrderBy(row => row.StudentName)
                .ToList(),
            entries
                .GroupBy(entry => entry.SubjectId)
                .Select(group => new LearningTimeSubjectTotal(
                    group.Key,
                    courses.TryGetValue(group.Key, out var subject) ? subject : group.First().Subject,
                    group.Sum(entry => entry.Minutes)))
                .OrderBy(row => row.Subject)
                .ToList(),
            entries
                .GroupBy(entry => entry.Date.Date)
                .Select(group => new LearningTimeDateTotal(group.Key, group.Sum(entry => entry.Minutes)))
                .OrderBy(row => row.Date)
                .ToList());
    }

    public async Task<LearningTimeEntry?> CreateFromAssignmentCompletionAsync(Assignment assignment)
    {
        if (!assignment.EstimatedMinutes.HasValue || assignment.EstimatedMinutes.Value <= 0)
        {
            return null;
        }

        if (await _learningTimeRepository.GetBySourceAsync(LearningTimeSource.Assignment, assignment.Id) is { } existing)
        {
            return existing;
        }

        return await CreateEntryAsync(new LearningTimeEntry
        {
            StudentId = assignment.StudentId,
            SubjectId = assignment.CourseId,
            Subject = assignment.Subject,
            Date = DateTime.Today,
            Minutes = assignment.EstimatedMinutes.Value,
            Notes = $"Completed assignment: {assignment.Title}",
            Source = LearningTimeSource.Assignment,
            SourceId = assignment.Id
        });
    }

    public async Task<LearningTimeEntry?> CreateFromLessonCompletionAsync(LessonPlan lessonPlan)
    {
        if (lessonPlan.EstimatedMinutes <= 0)
        {
            return null;
        }

        if (await _learningTimeRepository.GetBySourceAsync(LearningTimeSource.LessonPlan, lessonPlan.Id) is { } existing)
        {
            return existing;
        }

        return await CreateEntryAsync(new LearningTimeEntry
        {
            StudentId = lessonPlan.StudentId,
            SubjectId = lessonPlan.SubjectId,
            Date = lessonPlan.PlannedDate.Date,
            Minutes = lessonPlan.EstimatedMinutes,
            Notes = $"Completed lesson: {lessonPlan.Title}",
            Source = LearningTimeSource.LessonPlan,
            SourceId = lessonPlan.Id
        });
    }

    private async Task ValidateEntryAsync(LearningTimeEntry entry)
    {
        if (entry.Minutes <= 0)
        {
            throw new InvalidOperationException("Minutes must be positive.");
        }

        if (entry.StudentId <= 0 || !await _studentRepository.ExistsAsync(entry.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        if (entry.SubjectId <= 0 || !await _courseRepository.ExistsAsync(entry.SubjectId))
        {
            throw new InvalidOperationException("A valid subject is required.");
        }
    }

    private static void Normalize(LearningTimeEntry entry)
    {
        entry.Date = entry.Date.Date;
        entry.Subject = entry.Subject?.Trim() ?? string.Empty;
        entry.Notes = entry.Notes?.Trim() ?? string.Empty;
    }
}
