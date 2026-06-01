namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;

public class CurriculumResource
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Subject is required.")]
    public int SubjectId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    public CurriculumResourceType ResourceType { get; set; } = CurriculumResourceType.Book;

    [StringLength(200)]
    public string Publisher { get; set; } = string.Empty;

    [StringLength(200)]
    public string Author { get; set; } = string.Empty;

    [StringLength(500)]
    public string Url { get; set; } = string.Empty;

    [StringLength(50)]
    public string GradeLevel { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Course Course { get; set; } = null!;
    public List<StudentCurriculum> StudentCurricula { get; set; } = new();
}

public enum CurriculumResourceType
{
    Book,
    Workbook,
    OnlineCourse,
    Video,
    UnitStudy,
    Website,
    Other
}
