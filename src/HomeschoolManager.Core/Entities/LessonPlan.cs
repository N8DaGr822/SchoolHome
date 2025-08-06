namespace HomeschoolManager.Core.Entities;

public class LessonPlan
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Objectives { get; set; } = string.Empty;
    public string Materials { get; set; } = string.Empty;
    public string Activities { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int WeekNumber { get; set; }
    public int DayNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
} 