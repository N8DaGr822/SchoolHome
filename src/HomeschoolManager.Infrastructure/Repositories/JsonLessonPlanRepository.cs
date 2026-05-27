using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonLessonPlanRepository : ILessonPlanRepository
{
    private readonly HomeschoolDataStore _store;

    public JsonLessonPlanRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<LessonPlan?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var lessonPlan = data.LessonPlans.FirstOrDefault(lp => lp.Id == id);
        return lessonPlan == null ? null : HomeschoolDataStore.Clone(lessonPlan);
    }

    public async Task<IEnumerable<LessonPlan>> GetAllAsync()
    {
        var data = await _store.ReadAsync();
        return data.LessonPlans
            .OrderBy(lp => lp.PlannedDate)
            .ThenBy(lp => lp.Title)
            .Select(HomeschoolDataStore.Clone)
            .ToList();
    }

    public async Task<LessonPlan> AddAsync(LessonPlan entity)
    {
        var saved = HomeschoolDataStore.Clone(entity);
        await _store.WriteAsync(data =>
        {
            saved.Id = saved.Id == 0 ? NextId(data.LessonPlans.Select(lp => lp.Id)) : saved.Id;
            saved.FamilyId = saved.FamilyId == 0 ? 1 : saved.FamilyId;
            saved.CourseId = saved.SubjectId;
            saved.DurationMinutes = saved.DurationMinutes == 0 ? saved.EstimatedMinutes : saved.DurationMinutes;
            saved.WeekNumber = saved.WeekNumber == 0 ? 1 : saved.WeekNumber;
            saved.DayNumber = saved.DayNumber == 0 ? Math.Max(1, (int)saved.PlannedDate.DayOfWeek) : saved.DayNumber;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            data.LessonPlans.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(LessonPlan entity)
    {
        var updated = HomeschoolDataStore.Clone(entity);
        await _store.WriteAsync(data =>
        {
            var index = data.LessonPlans.FindIndex(lp => lp.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Lesson plan {updated.Id} was not found.");
            }

            updated.FamilyId = updated.FamilyId == 0 ? 1 : updated.FamilyId;
            updated.CourseId = updated.SubjectId;
            updated.DurationMinutes = updated.DurationMinutes == 0 ? updated.EstimatedMinutes : updated.DurationMinutes;
            updated.WeekNumber = updated.WeekNumber == 0 ? 1 : updated.WeekNumber;
            updated.DayNumber = updated.DayNumber == 0 ? Math.Max(1, (int)updated.PlannedDate.DayOfWeek) : updated.DayNumber;
            updated.CreatedAt = updated.CreatedAt == default ? data.LessonPlans[index].CreatedAt : updated.CreatedAt;
            data.LessonPlans[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data => data.LessonPlans.RemoveAll(lp => lp.Id == id));
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.LessonPlans.Any(lp => lp.Id == id);
    }

    public async Task<IEnumerable<LessonPlan>> GetByWeekAsync(DateTime weekStart, int? studentId = null, int? subjectId = null)
    {
        var start = weekStart.Date;
        var end = start.AddDays(7);
        var data = await _store.ReadAsync();
        return data.LessonPlans
            .Where(lp => lp.PlannedDate.Date >= start && lp.PlannedDate.Date < end)
            .Where(lp => !studentId.HasValue || lp.StudentId == studentId.Value)
            .Where(lp => !subjectId.HasValue || lp.SubjectId == subjectId.Value)
            .OrderBy(lp => lp.PlannedDate)
            .ThenBy(lp => lp.Title)
            .Select(HomeschoolDataStore.Clone)
            .ToList();
    }

    public async Task<IEnumerable<LessonPlan>> GetByStudentIdAsync(int studentId)
    {
        var data = await _store.ReadAsync();
        return data.LessonPlans
            .Where(lp => lp.StudentId == studentId)
            .OrderBy(lp => lp.PlannedDate)
            .ThenBy(lp => lp.Title)
            .Select(HomeschoolDataStore.Clone)
            .ToList();
    }

    public async Task<IEnumerable<LessonPlan>> GetBySubjectIdAsync(int subjectId)
    {
        var data = await _store.ReadAsync();
        return data.LessonPlans
            .Where(lp => lp.SubjectId == subjectId)
            .OrderBy(lp => lp.PlannedDate)
            .ThenBy(lp => lp.Title)
            .Select(HomeschoolDataStore.Clone)
            .ToList();
    }

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
