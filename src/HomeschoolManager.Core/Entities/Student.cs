namespace HomeschoolManager.Core.Entities;

public class Student
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string GradeLevel { get; set; } = string.Empty;
    public double GPA { get; set; }
    public int TotalCredits { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public List<Course> Courses { get; set; } = new();
    public List<Assignment> Assignments { get; set; } = new();
    public List<Grade> Grades { get; set; } = new();
} 