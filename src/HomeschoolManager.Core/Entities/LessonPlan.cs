namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;
using HomeschoolManager.Core.Interfaces;

public class LessonPlan : IEntity
{
    public int Id { get; set; }

    public int FamilyId { get; set; } = 1;

    public int StudentId { get; set; }

    public int SubjectId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime PlannedDate { get; set; } = DateTime.Today;

    [Range(1, 600)]
    public int EstimatedMinutes { get; set; } = 30;

    public LessonPlanStatus Status { get; set; } = LessonPlanStatus.Planned;

    public int? AssignmentId { get; set; }

    [StringLength(1000)]
    public string Objectives { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Materials { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Activities { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Assessment { get; set; } = string.Empty;

    [Range(1, 600)]
    public int DurationMinutes { get; set; } = 30;

    [Range(1, 52)]
    public int WeekNumber { get; set; } = 1;

    [Range(1, 7)]
    public int DayNumber { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}

public enum LessonPlanStatus
{
    Planned,
    Completed,
    Skipped
}
