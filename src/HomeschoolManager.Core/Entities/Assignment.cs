namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;

public class Assignment
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime DueDate { get; set; }

    public DateTime AssignedDate { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;

    [Range(1, int.MaxValue, ErrorMessage = "Course is required.")]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Student is required.")]
    public int StudentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Additional properties for UI compatibility
    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Estimated minutes must be positive.")]
    public int? EstimatedMinutes { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public List<Grade> Grades { get; set; } = new();
}

public enum AssignmentStatus
{
    Assigned,
    InProgress,
    Completed,
    Overdue
}
