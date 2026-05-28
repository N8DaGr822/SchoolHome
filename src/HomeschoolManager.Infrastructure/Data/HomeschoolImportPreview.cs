namespace HomeschoolManager.Infrastructure.Data;

public sealed record HomeschoolImportPreview(
    int SchemaVersion,
    int StudentCount,
    int CourseCount,
    int LessonPlanCount,
    int AssignmentCount,
    int GradeCount,
    int AttendanceRecordCount,
    int LearningTimeEntryCount);
