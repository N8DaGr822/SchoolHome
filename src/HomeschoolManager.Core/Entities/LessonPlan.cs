namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;

public class LessonPlan
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Objectives { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Materials { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Activities { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Assessment { get; set; } = string.Empty;

    [Range(1, 600)]
    public int DurationMinutes { get; set; }

    [Range(1, 52)]
    public int WeekNumber { get; set; }

    [Range(1, 7)]
    public int DayNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}
