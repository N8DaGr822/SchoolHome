using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonLearningTimeRepository : JsonRepositoryBase<LearningTimeEntry>, ILearningTimeRepository
{
    public JsonLearningTimeRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<LearningTimeEntry> Items(HomeschoolData data) => data.LearningTimeEntries;

    protected override string EntityLabel => "Learning time entry";

    private protected override LearningTimeEntry Hydrate(HomeschoolData data, LearningTimeEntry entity) =>
        RepositoryProjection.HydrateLearningTimeEntry(data, entity);

    protected override LearningTimeEntry Normalize(LearningTimeEntry entity)
    {
        entity.Date = entity.Date.Date;
        entity.Subject = entity.Subject?.Trim() ?? string.Empty;
        entity.Notes = entity.Notes?.Trim() ?? string.Empty;
        if (entity.Minutes <= 0)
        {
            throw new InvalidOperationException("Minutes must be positive.");
        }

        return entity;
    }

    private protected override IEnumerable<LearningTimeEntry> Order(HomeschoolData data, IEnumerable<LearningTimeEntry> items) =>
        items.OrderByDescending(e => e.Date)
            .ThenBy(e => GetStudentName(data, e.StudentId))
            .ThenBy(e => e.Subject);

    private protected override void Validate(HomeschoolData data, LearningTimeEntry entity)
    {
        if (entity.StudentId <= 0 || !data.Students.Any(s => s.Id == entity.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        if (entity.SubjectId <= 0 || !data.Courses.Any(c => c.Id == entity.SubjectId))
        {
            throw new InvalidOperationException("A valid subject is required.");
        }
    }

    private protected override void OnSaving(HomeschoolData data, LearningTimeEntry entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.Subject))
        {
            return;
        }

        var course = data.Courses.FirstOrDefault(c => c.Id == entity.SubjectId);
        entity.Subject = course == null ? string.Empty : string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject;
    }

    public async Task<IEnumerable<LearningTimeEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        if (end < start)
        {
            (start, end) = (end, start);
        }

        var data = await Store.ReadAsync();
        return data.LearningTimeEntries
            .Where(e => e.Date.Date >= start && e.Date.Date <= end)
            .OrderBy(e => e.Date)
            .ThenBy(e => GetStudentName(data, e.StudentId))
            .ThenBy(e => e.Subject)
            .Select(e => RepositoryProjection.HydrateLearningTimeEntry(data, e))
            .ToList();
    }

    public async Task<IEnumerable<LearningTimeEntry>> GetByStudentIdAsync(int studentId)
    {
        var data = await Store.ReadAsync();
        return data.LearningTimeEntries
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.Date)
            .ThenBy(e => e.Subject)
            .Select(e => RepositoryProjection.HydrateLearningTimeEntry(data, e))
            .ToList();
    }

    public async Task<LearningTimeEntry?> GetBySourceAsync(LearningTimeSource source, int sourceId)
    {
        var data = await Store.ReadAsync();
        var entry = data.LearningTimeEntries.FirstOrDefault(e => e.Source == source && e.SourceId == sourceId);
        return entry == null ? null : RepositoryProjection.HydrateLearningTimeEntry(data, entry);
    }

    private static string GetStudentName(HomeschoolData data, int studentId)
    {
        var student = data.Students.FirstOrDefault(s => s.Id == studentId);
        return student == null ? string.Empty : $"{student.LastName}, {student.FirstName}";
    }
}
