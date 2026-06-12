namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;
using HomeschoolManager.Core.Interfaces;

public class StudentCurriculum : IEntity
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Student is required.")]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Curriculum resource is required.")]
    public int CurriculumResourceId { get; set; }

    public CurriculumStatus Status { get; set; } = CurriculumStatus.NotStarted;

    [StringLength(100)]
    public string CurrentUnit { get; set; } = string.Empty;

    [StringLength(100)]
    public string CurrentLesson { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "Percent complete must be between 0 and 100.")]
    public int PercentComplete { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? TargetEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student Student { get; set; } = null!;
    public CurriculumResource CurriculumResource { get; set; } = null!;
}

public enum CurriculumStatus
{
    NotStarted,
    InProgress,
    Completed,
    Paused,
    Dropped
}
