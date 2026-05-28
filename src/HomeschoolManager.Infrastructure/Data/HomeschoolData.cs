using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Infrastructure.Data;

internal class HomeschoolData
{
    public const int CurrentSchemaVersion = 3;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public List<Student> Students { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public List<LessonPlan> LessonPlans { get; set; } = new();
    public List<Assignment> Assignments { get; set; } = new();
    public List<Grade> Grades { get; set; } = new();
    public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
    public List<LearningTimeEntry> LearningTimeEntries { get; set; } = new();
}
