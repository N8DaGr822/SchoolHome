namespace HomeschoolManager.Core.Entities;

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime AssignedDate { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;
    public int CourseId { get; set; }
    public int StudentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Additional properties for UI compatibility
    public string Subject { get; set; } = string.Empty;
    
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