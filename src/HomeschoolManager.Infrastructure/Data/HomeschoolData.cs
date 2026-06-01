using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Infrastructure.Data;

internal class HomeschoolData
{
    public const int CurrentSchemaVersion = 7;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public List<Student> Students { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public List<LessonPlan> LessonPlans { get; set; } = new();
    public List<Assignment> Assignments { get; set; } = new();
    public List<Grade> Grades { get; set; } = new();
    public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
    public List<LearningTimeEntry> LearningTimeEntries { get; set; } = new();
    public List<PortfolioItem> PortfolioItems { get; set; } = new();
    public List<CurriculumResource> CurriculumResources { get; set; } = new();
    public List<StudentCurriculum> StudentCurricula { get; set; } = new();
    public List<ParentNote> ParentNotes { get; set; } = new();
    public List<Yearbook> Yearbooks { get; set; } = new();
    public List<YearbookPage> YearbookPages { get; set; } = new();
    public List<YearbookAsset> YearbookAssets { get; set; } = new();
}
