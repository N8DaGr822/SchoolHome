namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;
using HomeschoolManager.Core.Interfaces;

public class LearningTimeEntry : IEntity
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Student is required.")]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Subject is required.")]
    public int SubjectId { get; set; }

    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    [Range(1, 1440, ErrorMessage = "Minutes must be positive.")]
    public int Minutes { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;

    public LearningTimeSource Source { get; set; } = LearningTimeSource.Manual;
    public int? SourceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}

public enum LearningTimeSource
{
    Manual,
    Assignment,
    LessonPlan
}
