namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;
using HomeschoolManager.Core.Interfaces;

public class Yearbook : IEntity
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Family is required.")]
    public int FamilyId { get; set; } = 1;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string SchoolYear { get; set; } = string.Empty;

    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    public YearbookScope Scope { get; set; } = YearbookScope.Family;
    public int? StudentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student? Student { get; set; }
    public List<YearbookPage> Pages { get; set; } = new();
    public List<YearbookAsset> Assets { get; set; } = new();
}

public enum YearbookScope
{
    Family,
    Student
}
