using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonLessonPlanRepository : JsonRepositoryBase<LessonPlan>, ILessonPlanRepository
{
    public JsonLessonPlanRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<LessonPlan> Items(HomeschoolData data) => data.LessonPlans;

    protected override string EntityLabel => "Lesson plan";

    private protected override LessonPlan Hydrate(HomeschoolData data, LessonPlan entity) =>
        HomeschoolDataStore.Clone(entity);

    private protected override IEnumerable<LessonPlan> Order(HomeschoolData data, IEnumerable<LessonPlan> items) =>
        items.OrderBy(lp => lp.PlannedDate).ThenBy(lp => lp.Title);

    private protected override void OnSaving(HomeschoolData data, LessonPlan entity)
    {
        entity.FamilyId = entity.FamilyId == 0 ? 1 : entity.FamilyId;
        entity.CourseId = entity.SubjectId;
        entity.DurationMinutes = entity.DurationMinutes == 0 ? entity.EstimatedMinutes : entity.DurationMinutes;
        entity.WeekNumber = entity.WeekNumber == 0 ? 1 : entity.WeekNumber;
        entity.DayNumber = entity.DayNumber == 0 ? Math.Max(1, (int)entity.PlannedDate.DayOfWeek) : entity.DayNumber;
    }

    public async Task<IEnumerable<LessonPlan>> GetByWeekAsync(DateTime weekStart, int? studentId = null, int? subjectId = null)
    {
        var start = weekStart.Date;
        var end = start.AddDays(7);
        var data = await Store.ReadAsync();
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
        var data = await Store.ReadAsync();
        return data.LessonPlans
            .Where(lp => lp.StudentId == studentId)
            .OrderBy(lp => lp.PlannedDate)
            .ThenBy(lp => lp.Title)
            .Select(HomeschoolDataStore.Clone)
            .ToList();
    }

    public async Task<IEnumerable<LessonPlan>> GetBySubjectIdAsync(int subjectId)
    {
        var data = await Store.ReadAsync();
        return data.LessonPlans
            .Where(lp => lp.SubjectId == subjectId)
            .OrderBy(lp => lp.PlannedDate)
            .ThenBy(lp => lp.Title)
            .Select(HomeschoolDataStore.Clone)
            .ToList();
    }
}
