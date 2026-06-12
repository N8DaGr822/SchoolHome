using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Core.Entities;

public class Grade : IEntity
{
    public int Id { get; set; }
    public decimal Score { get; set; }
    public string? Comments { get; set; }
    public DateTime GradedDate { get; set; }
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Denormalized display fields
    public string Subject { get; set; } = string.Empty;
    public string Assignment { get; set; } = string.Empty;
    public string GradeValue { get; set; } = string.Empty;

    // Navigation properties
    public Assignment AssignmentEntity { get; set; } = null!;
    public Student Student { get; set; } = null!;
}