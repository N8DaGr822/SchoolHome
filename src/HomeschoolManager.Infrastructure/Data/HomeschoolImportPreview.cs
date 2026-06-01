namespace HomeschoolManager.Infrastructure.Data;

public sealed record HomeschoolImportPreview(
    int SchemaVersion,
    int StudentCount,
    int CourseCount,
    int LessonPlanCount,
    int AssignmentCount,
    int GradeCount,
    int AttendanceRecordCount,
    int LearningTimeEntryCount,
    int PortfolioItemCount,
    int CurriculumResourceCount,
    int StudentCurriculumCount,
    int ParentNoteCount,
    int YearbookCount,
    int YearbookPageCount,
    int YearbookAssetCount);
