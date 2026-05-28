using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonLearningTimeRepository : ILearningTimeRepository
{
    private readonly HomeschoolDataStore _store;

    public JsonLearningTimeRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<LearningTimeEntry?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var entry = data.LearningTimeEntries.FirstOrDefault(e => e.Id == id);
        return entry == null ? null : RepositoryProjection.HydrateLearningTimeEntry(data, entry);
    }

    public async Task<IEnumerable<LearningTimeEntry>> GetAllAsync()
    {
        var data = await _store.ReadAsync();
        return data.LearningTimeEntries
            .OrderByDescending(e => e.Date)
            .ThenBy(e => GetStudentName(data, e.StudentId))
            .ThenBy(e => e.Subject)
            .Select(e => RepositoryProjection.HydrateLearningTimeEntry(data, e))
            .ToList();
    }

    public async Task<LearningTimeEntry> AddAsync(LearningTimeEntry entity)
    {
        var saved = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            ValidateReferences(data, saved);
            saved.Id = saved.Id == 0 ? NextId(data.LearningTimeEntries.Select(e => e.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            FillSubject(data, saved);
            data.LearningTimeEntries.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(LearningTimeEntry entity)
    {
        var updated = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            var index = data.LearningTimeEntries.FindIndex(e => e.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Learning time entry {updated.Id} was not found.");
            }

            ValidateReferences(data, updated);
            updated.CreatedAt = updated.CreatedAt == default ? data.LearningTimeEntries[index].CreatedAt : updated.CreatedAt;
            FillSubject(data, updated);
            data.LearningTimeEntries[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data => data.LearningTimeEntries.RemoveAll(e => e.Id == id));
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.LearningTimeEntries.Any(e => e.Id == id);
    }

    public async Task<IEnumerable<LearningTimeEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        if (end < start)
        {
            (start, end) = (end, start);
        }

        var data = await _store.ReadAsync();
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
        var data = await _store.ReadAsync();
        return data.LearningTimeEntries
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.Date)
            .ThenBy(e => e.Subject)
            .Select(e => RepositoryProjection.HydrateLearningTimeEntry(data, e))
            .ToList();
    }

    public async Task<LearningTimeEntry?> GetBySourceAsync(LearningTimeSource source, int sourceId)
    {
        var data = await _store.ReadAsync();
        var entry = data.LearningTimeEntries.FirstOrDefault(e => e.Source == source && e.SourceId == sourceId);
        return entry == null ? null : RepositoryProjection.HydrateLearningTimeEntry(data, entry);
    }

    private static LearningTimeEntry Normalize(LearningTimeEntry entry)
    {
        entry.Date = entry.Date.Date;
        entry.Subject = entry.Subject?.Trim() ?? string.Empty;
        entry.Notes = entry.Notes?.Trim() ?? string.Empty;
        if (entry.Minutes <= 0)
        {
            throw new InvalidOperationException("Minutes must be positive.");
        }

        return entry;
    }

    private static void ValidateReferences(HomeschoolData data, LearningTimeEntry entry)
    {
        if (entry.StudentId <= 0 || !data.Students.Any(s => s.Id == entry.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        if (entry.SubjectId <= 0 || !data.Courses.Any(c => c.Id == entry.SubjectId))
        {
            throw new InvalidOperationException("A valid subject is required.");
        }
    }

    private static void FillSubject(HomeschoolData data, LearningTimeEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Subject))
        {
            return;
        }

        var course = data.Courses.FirstOrDefault(c => c.Id == entry.SubjectId);
        entry.Subject = course == null ? string.Empty : string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject;
    }

    private static string GetStudentName(HomeschoolData data, int studentId)
    {
        var student = data.Students.FirstOrDefault(s => s.Id == studentId);
        return student == null ? string.Empty : $"{student.LastName}, {student.FirstName}";
    }

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
