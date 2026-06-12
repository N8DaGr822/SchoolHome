namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;
using HomeschoolManager.Core.Interfaces;

public class PortfolioItem : IEntity
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Student is required.")]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Subject is required.")]
    public int SubjectId { get; set; }

    public PortfolioItemType Type { get; set; } = PortfolioItemType.Note;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Today;
    public bool IsBestWork { get; set; }
    public int? AssignmentId { get; set; }
    public int? LessonPlanId { get; set; }

    [StringLength(500)]
    public string ExternalUrl { get; set; } = string.Empty;

    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [StringLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [StringLength(500)]
    public string StoredFilePath { get; set; } = string.Empty;

    [StringLength(255)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [StringLength(500)]
    public string Tags { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public Assignment? Assignment { get; set; }
    public LessonPlan? LessonPlan { get; set; }
}

public enum PortfolioItemType
{
    Photo,
    Pdf,
    Document,
    Link,
    Video,
    Note
}
