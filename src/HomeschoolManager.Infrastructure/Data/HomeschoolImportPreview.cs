namespace HomeschoolManager.Infrastructure.Data;

public sealed record HomeschoolImportPreview(
    int SchemaVersion,
    int StudentCount,
    int CourseCount,
    int AssignmentCount,
    int GradeCount);
