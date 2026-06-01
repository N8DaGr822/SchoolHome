namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;

public class ParentNote
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Student is required.")]
    public int StudentId { get; set; }

    public int? SubjectId { get; set; }
    public int? AssignmentId { get; set; }
    public int? LessonPlanId { get; set; }

    public ParentNoteCategory Category { get; set; } = ParentNoteCategory.General;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(3000)]
    public string Content { get; set; } = string.Empty;

    public DateTime NoteDate { get; set; } = DateTime.Today;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student Student { get; set; } = null!;
    public Course? Course { get; set; }
    public Assignment? Assignment { get; set; }
    public LessonPlan? LessonPlan { get; set; }
}

public enum ParentNoteCategory
{
    General,
    Struggle,
    Breakthrough,
    Behavior,
    Assessment,
    Planning
}
