using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class ProgressReportService : IProgressReportService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ILessonPlanRepository _lessonPlanRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ILearningTimeRepository _learningTimeRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IParentNoteRepository _parentNoteRepository;

    public ProgressReportService(
        IStudentRepository studentRepository,
        IAssignmentRepository assignmentRepository,
        ILessonPlanRepository lessonPlanRepository,
        IAttendanceRepository attendanceRepository,
        ILearningTimeRepository learningTimeRepository,
        IPortfolioRepository portfolioRepository,
        IParentNoteRepository parentNoteRepository)
    {
        _studentRepository = studentRepository;
        _assignmentRepository = assignmentRepository;
        _lessonPlanRepository = lessonPlanRepository;
        _attendanceRepository = attendanceRepository;
        _learningTimeRepository = learningTimeRepository;
        _portfolioRepository = portfolioRepository;
        _parentNoteRepository = parentNoteRepository;
    }

    public async Task<ProgressReport> GenerateAsync(int studentId, DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;

        if (studentId <= 0)
        {
            throw new InvalidOperationException("Student is required.");
        }

        if (end < start)
        {
            throw new InvalidOperationException("End date must be on or after start date.");
        }

        var student = await _studentRepository.GetByIdAsync(studentId)
            ?? throw new InvalidOperationException("Student was not found.");

        var assignments = (await _assignmentRepository.GetByStudentIdAsync(studentId))
            .Where(a => a.Status == AssignmentStatus.Completed && IsWithinRange(GetAssignmentCompletionDate(a), start, end))
            .OrderBy(a => GetAssignmentCompletionDate(a))
            .ThenBy(a => a.Title)
            .ToList();

        var lessons = (await _lessonPlanRepository.GetByStudentIdAsync(studentId))
            .Where(lp => lp.Status == LessonPlanStatus.Completed && IsWithinRange(GetLessonCompletionDate(lp), start, end))
            .OrderBy(lp => GetLessonCompletionDate(lp))
            .ThenBy(lp => lp.Title)
            .ToList();

        var attendanceRecords = (await _attendanceRepository.GetByDateRangeAsync(start, end))
            .Where(a => a.StudentId == studentId)
            .OrderBy(a => a.Date)
            .ToList();

        var learningTimeEntries = (await _learningTimeRepository.GetByDateRangeAsync(start, end))
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Subject)
            .ToList();

        var bestWorkItems = (await _portfolioRepository.GetFilteredAsync(new PortfolioFilter(
                StudentId: studentId,
                StartDate: start,
                EndDate: end,
                BestWorkOnly: true)))
            .OrderBy(i => i.Date)
            .ThenBy(i => i.Title)
            .ToList();
        var parentNotes = (await _parentNoteRepository.GetFilteredAsync(new ParentNoteFilter(
                StudentId: studentId,
                StartDate: start,
                EndDate: end)))
            .OrderBy(n => n.NoteDate)
            .ThenBy(n => n.Title)
            .ToList();

        var totalLearningMinutes = learningTimeEntries.Sum(e => e.Minutes);
        var summary = new ProgressReportSummary(
            assignments.Count,
            lessons.Count,
            attendanceRecords.Count,
            attendanceRecords.Count(a => a.Status is AttendanceStatus.Present or AttendanceStatus.FieldTrip or AttendanceStatus.Partial),
            attendanceRecords.Count(a => a.Status == AttendanceStatus.Absent),
            totalLearningMinutes,
            Math.Round(totalLearningMinutes / 60.0, 2),
            bestWorkItems.Count);

        return new ProgressReport(
            student,
            start,
            end,
            summary,
            assignments,
            lessons,
            attendanceRecords,
            learningTimeEntries,
            bestWorkItems,
            BuildSubjectSummaries(learningTimeEntries),
            BuildNotes(attendanceRecords, learningTimeEntries, bestWorkItems, parentNotes));
    }

    private static IReadOnlyList<ProgressReportSubjectSummary> BuildSubjectSummaries(IEnumerable<LearningTimeEntry> entries)
    {
        return entries
            .GroupBy(e => new { e.SubjectId, Subject = string.IsNullOrWhiteSpace(e.Subject) ? "Unspecified" : e.Subject })
            .Select(group =>
            {
                var minutes = group.Sum(e => e.Minutes);
                return new ProgressReportSubjectSummary(group.Key.SubjectId, group.Key.Subject, minutes, Math.Round(minutes / 60.0, 2));
            })
            .OrderBy(row => row.Subject)
            .ToList();
    }

    private static IReadOnlyList<ProgressReportNote> BuildNotes(
        IEnumerable<AttendanceRecord> attendanceRecords,
        IEnumerable<LearningTimeEntry> learningTimeEntries,
        IEnumerable<PortfolioItem> bestWorkItems,
        IEnumerable<ParentNote> parentNotes)
    {
        return attendanceRecords
            .Where(a => !string.IsNullOrWhiteSpace(a.Notes))
            .Select(a => new ProgressReportNote(a.Date.Date, "Attendance", a.Notes))
            .Concat(learningTimeEntries
                .Where(e => !string.IsNullOrWhiteSpace(e.Notes))
                .Select(e => new ProgressReportNote(e.Date.Date, "Learning Time", e.Notes)))
            .Concat(bestWorkItems
                .Where(i => !string.IsNullOrWhiteSpace(i.Notes))
                .Select(i => new ProgressReportNote(i.Date.Date, "Portfolio", i.Notes)))
            .Concat(parentNotes
                .Select(n => new ProgressReportNote(n.NoteDate.Date, $"Parent Note - {GetCategoryLabel(n.Category)}", $"{n.Title}: {n.Content}")))
            .OrderBy(note => note.Date)
            .ThenBy(note => note.Source)
            .ToList();
    }

    private static string GetCategoryLabel(ParentNoteCategory category)
    {
        return category switch
        {
            ParentNoteCategory.Breakthrough => "Breakthrough",
            _ => category.ToString()
        };
    }

    private static DateTime GetAssignmentCompletionDate(Assignment assignment)
    {
        return (assignment.UpdatedAt ?? assignment.DueDate).Date;
    }

    private static DateTime GetLessonCompletionDate(LessonPlan lessonPlan)
    {
        return (lessonPlan.UpdatedAt ?? lessonPlan.PlannedDate).Date;
    }

    private static bool IsWithinRange(DateTime date, DateTime start, DateTime end)
    {
        return date.Date >= start && date.Date <= end;
    }
}
