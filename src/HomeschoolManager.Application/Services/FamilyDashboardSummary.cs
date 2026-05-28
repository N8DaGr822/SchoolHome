using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public static class FamilyDashboardSummary
{
    public static IReadOnlyList<StudentDashboardSummary> BuildStudentCards(
        IEnumerable<Student> students,
        IEnumerable<Assignment> assignments,
        IEnumerable<LessonPlan> lessonPlans,
        DateTime date,
        IEnumerable<AttendanceRecord>? attendanceRecords = null)
    {
        var targetDate = date.Date;
        var assignmentList = assignments.ToList();
        var lessonList = lessonPlans.ToList();
        var attendanceList = attendanceRecords?.ToList();

        return students
            .Select(student =>
            {
                var studentAssignments = assignmentList.Where(a => a.StudentId == student.Id).ToList();
                var dueToday = studentAssignments.Count(a => a.Status != AssignmentStatus.Completed && a.DueDate.Date == targetDate);
                var completed = studentAssignments.Count(a => a.Status == AssignmentStatus.Completed);
                var remaining = studentAssignments.Count(a => a.Status != AssignmentStatus.Completed);
                var overdue = studentAssignments.Count(a => a.Status != AssignmentStatus.Completed && a.DueDate.Date < targetDate);
                var lessonsToday = lessonList.Where(lp => lp.StudentId == student.Id && lp.PlannedDate.Date == targetDate).ToList();
                var completionPercent = studentAssignments.Count == 0
                    ? 100
                    : (int)Math.Round((double)completed / studentAssignments.Count * 100);

                return new StudentDashboardSummary(
                    student.Id,
                    $"{student.FirstName} {student.LastName}",
                    student.GradeLevel,
                    completionPercent,
                    dueToday,
                    lessonsToday.Count,
                    completed,
                    remaining,
                    overdue,
                    GetAttendanceStatus(student.Id, targetDate, lessonsToday, attendanceList));
            })
            .OrderBy(card => card.StudentName)
            .ToList();
    }

    public static IReadOnlyList<DashboardAttentionItem> BuildAttentionItems(
        IEnumerable<StudentDashboardSummary> studentCards,
        IEnumerable<Assignment> assignments,
        DateTime date)
    {
        var targetDate = date.Date;
        var studentNames = studentCards.ToDictionary(card => card.StudentId, card => card.StudentName);

        return assignments
            .Where(a => a.Status != AssignmentStatus.Completed && a.DueDate.Date < targetDate)
            .Select(a => new DashboardAttentionItem(
                studentNames.TryGetValue(a.StudentId, out var studentName) ? studentName : "Unknown student",
                a.Title,
                "Overdue Assignment",
                a.DueDate.Date))
            .OrderBy(item => item.Date)
            .ThenBy(item => item.StudentName)
            .ToList();
    }

    private static string GetAttendanceStatus(
        int studentId,
        DateTime targetDate,
        IReadOnlyCollection<LessonPlan> lessonsToday,
        IReadOnlyCollection<AttendanceRecord>? attendanceRecords)
    {
        if (attendanceRecords is not null)
        {
            var attendanceRecord = attendanceRecords.FirstOrDefault(a =>
                a.StudentId == studentId && a.Date.Date == targetDate);

            return attendanceRecord == null
                ? "Not Recorded"
                : AttendanceStatusDisplay.GetLabel(attendanceRecord.Status);
        }

        return GetLessonBasedAttendanceStatus(lessonsToday);
    }

    private static string GetLessonBasedAttendanceStatus(IReadOnlyCollection<LessonPlan> lessonsToday)
    {
        if (!lessonsToday.Any())
        {
            return "Not Recorded";
        }

        if (lessonsToday.Any(lp => lp.Status == LessonPlanStatus.Completed))
        {
            return "Present";
        }

        return lessonsToday.All(lp => lp.Status == LessonPlanStatus.Skipped)
            ? "Skipped"
            : "Planned";
    }
}

public sealed record StudentDashboardSummary(
    int StudentId,
    string StudentName,
    string GradeLevel,
    int CompletionPercent,
    int AssignmentsDueToday,
    int LessonsPlannedToday,
    int CompletedCount,
    int RemainingCount,
    int OverdueAssignmentCount,
    string AttendanceStatus);

public sealed record DashboardAttentionItem(
    string StudentName,
    string Title,
    string Type,
    DateTime Date);
