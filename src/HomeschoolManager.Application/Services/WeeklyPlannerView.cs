using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public static class WeeklyPlannerView
{
    public static IReadOnlyList<WeeklyPlannerDay> GroupByDay(IEnumerable<LessonPlan> lessons, DateTime weekStart)
    {
        var start = GetWeekStart(weekStart);
        var lessonLookup = lessons
            .GroupBy(lesson => lesson.PlannedDate.Date)
            .ToDictionary(group => group.Key, group => group.OrderBy(lesson => lesson.Title).ToList());

        return Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = start.AddDays(offset);
                lessonLookup.TryGetValue(date, out var dayLessons);
                return new WeeklyPlannerDay(date, dayLessons ?? new List<LessonPlan>());
            })
            .ToList();
    }

    public static DateTime GetWeekStart(DateTime date)
    {
        var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.Date.AddDays(-offset);
    }
}

public sealed record WeeklyPlannerDay(DateTime Date, IReadOnlyList<LessonPlan> Lessons);
