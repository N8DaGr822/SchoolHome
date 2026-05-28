using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class FamilyDashboardSummaryTests
{
    [Fact]
    public void BuildStudentCards_AggregatesTodayActivityByStudent()
    {
        var today = new DateTime(2026, 5, 27);
        var students = new[]
        {
            new Student { Id = 1, FirstName = "Ava", LastName = "Brown", GradeLevel = "4th" },
            new Student { Id = 2, FirstName = "Noah", LastName = "Green", GradeLevel = "6th" }
        };
        var assignments = new[]
        {
            new Assignment { Id = 1, StudentId = 1, Title = "Math", DueDate = today, Status = AssignmentStatus.Assigned },
            new Assignment { Id = 2, StudentId = 1, Title = "Essay", DueDate = today.AddDays(-1), Status = AssignmentStatus.Assigned },
            new Assignment { Id = 3, StudentId = 1, Title = "Quiz", DueDate = today.AddDays(-2), Status = AssignmentStatus.Completed },
            new Assignment { Id = 4, StudentId = 2, Title = "Reading", DueDate = today.AddDays(1), Status = AssignmentStatus.Assigned }
        };
        var lessons = new[]
        {
            new LessonPlan { Id = 1, StudentId = 1, PlannedDate = today, Status = LessonPlanStatus.Planned },
            new LessonPlan { Id = 2, StudentId = 1, PlannedDate = today, Status = LessonPlanStatus.Completed },
            new LessonPlan { Id = 3, StudentId = 2, PlannedDate = today.AddDays(1), Status = LessonPlanStatus.Planned }
        };

        var cards = FamilyDashboardSummary.BuildStudentCards(students, assignments, lessons, today).ToList();
        var ava = cards.Single(card => card.StudentId == 1);
        var noah = cards.Single(card => card.StudentId == 2);

        Assert.Equal(1, ava.AssignmentsDueToday);
        Assert.Equal(2, ava.LessonsPlannedToday);
        Assert.Equal(1, ava.CompletedCount);
        Assert.Equal(2, ava.RemainingCount);
        Assert.Equal(1, ava.OverdueAssignmentCount);
        Assert.Equal(33, ava.CompletionPercent);
        Assert.Equal("Present", ava.AttendanceStatus);

        Assert.Equal(0, noah.AssignmentsDueToday);
        Assert.Equal(0, noah.LessonsPlannedToday);
        Assert.Equal(1, noah.RemainingCount);
        Assert.Equal("Not Recorded", noah.AttendanceStatus);
    }

    [Fact]
    public void BuildAttentionItems_ReturnsOverdueAssignments()
    {
        var today = new DateTime(2026, 5, 27);
        var cards = new[]
        {
            new StudentDashboardSummary(1, "Ava Brown", "4th", 50, 0, 0, 1, 1, 1, "Not Recorded")
        };
        var assignments = new[]
        {
            new Assignment { Id = 1, StudentId = 1, Title = "Late Math", DueDate = today.AddDays(-1), Status = AssignmentStatus.Assigned },
            new Assignment { Id = 2, StudentId = 1, Title = "Done", DueDate = today.AddDays(-2), Status = AssignmentStatus.Completed }
        };

        var items = FamilyDashboardSummary.BuildAttentionItems(cards, assignments, today);

        Assert.Single(items);
        Assert.Equal("Ava Brown", items[0].StudentName);
        Assert.Equal("Late Math", items[0].Title);
        Assert.Equal("Overdue Assignment", items[0].Type);
    }

    [Fact]
    public void BuildStudentCards_RendersAttendanceRecordStatus()
    {
        var today = new DateTime(2026, 5, 27);
        var students = new[]
        {
            new Student { Id = 1, FirstName = "Ava", LastName = "Brown", GradeLevel = "4th" }
        };
        var attendanceRecords = new[]
        {
            new AttendanceRecord
            {
                Id = 1,
                StudentId = 1,
                Date = today,
                Status = AttendanceStatus.FieldTrip,
                Minutes = 180
            }
        };

        var cards = FamilyDashboardSummary.BuildStudentCards(
            students,
            Array.Empty<Assignment>(),
            Array.Empty<LessonPlan>(),
            today,
            attendanceRecords);

        Assert.Single(cards);
        Assert.Equal("Field Trip", cards[0].AttendanceStatus);
        Assert.Equal("info", AttendanceStatusDisplay.GetBadgeColor(AttendanceStatus.FieldTrip));
        Assert.Equal("FT", AttendanceStatusDisplay.GetShortLabel(AttendanceStatus.FieldTrip));
    }
}
