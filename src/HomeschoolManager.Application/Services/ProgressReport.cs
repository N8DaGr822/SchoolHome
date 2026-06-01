using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public sealed record ProgressReport(
    Student Student,
    DateTime StartDate,
    DateTime EndDate,
    ProgressReportSummary Summary,
    IReadOnlyList<Assignment> CompletedAssignments,
    IReadOnlyList<LessonPlan> CompletedLessons,
    IReadOnlyList<AttendanceRecord> AttendanceRecords,
    IReadOnlyList<LearningTimeEntry> LearningTimeEntries,
    IReadOnlyList<PortfolioItem> BestWorkItems,
    IReadOnlyList<ProgressReportSubjectSummary> LearningTimeBySubject,
    IReadOnlyList<ProgressReportNote> Notes);

public sealed record ProgressReportSummary(
    int CompletedAssignmentCount,
    int CompletedLessonCount,
    int AttendanceRecordCount,
    int PresentAttendanceCount,
    int AbsentAttendanceCount,
    int TotalLearningMinutes,
    double TotalLearningHours,
    int BestWorkItemCount);

public sealed record ProgressReportSubjectSummary(
    int SubjectId,
    string Subject,
    int Minutes,
    double Hours);

public sealed record ProgressReportNote(
    DateTime Date,
    string Source,
    string Text);
