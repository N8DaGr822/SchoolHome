namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;
using HomeschoolManager.Core.Interfaces;

public class YearbookPage : IEntity
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Yearbook is required.")]
    public int YearbookId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Sort order must be non-negative.")]
    public int SortOrder { get; set; }

    public bool IsHidden { get; set; }

    [Required]
    public string ContentJson { get; set; } = "{}";

    public List<PageElement> Elements { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Yearbook Yearbook { get; set; } = null!;
}
