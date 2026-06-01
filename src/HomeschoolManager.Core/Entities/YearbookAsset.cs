namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;

public class YearbookAsset
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Yearbook is required.")]
    public int YearbookId { get; set; }

    public int? YearbookPageId { get; set; }
    public int? PortfolioItemId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string SourcePath { get; set; } = string.Empty;

    [StringLength(500)]
    public string Caption { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Yearbook Yearbook { get; set; } = null!;
    public YearbookPage? Page { get; set; }
    public PortfolioItem? PortfolioItem { get; set; }
}
