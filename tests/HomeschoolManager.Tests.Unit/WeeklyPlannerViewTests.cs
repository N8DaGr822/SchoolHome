using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class WeeklyPlannerViewTests
{
    [Fact]
    public void GroupByDay_ReturnsSevenDaysStartingMonday()
    {
        var weekStart = new DateTime(2026, 5, 27);

        var days = WeeklyPlannerView.GroupByDay([], weekStart);

        Assert.Equal(7, days.Count);
        Assert.Equal(new DateTime(2026, 5, 25), days[0].Date);
        Assert.Equal(new DateTime(2026, 5, 31), days[6].Date);
    }

    [Fact]
    public void GroupByDay_PlacesLessonsOnPlannedDate()
    {
        var lesson = new LessonPlan
        {
            Id = 1,
            Title = "Fractions",
            PlannedDate = new DateTime(2026, 5, 28)
        };

        var days = WeeklyPlannerView.GroupByDay([lesson], new DateTime(2026, 5, 25));

        Assert.Empty(days[0].Lessons);
        Assert.Single(days[3].Lessons);
        Assert.Equal("Fractions", days[3].Lessons[0].Title);
    }
}
