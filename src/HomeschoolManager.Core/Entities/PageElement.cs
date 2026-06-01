namespace HomeschoolManager.Core.Entities;

using System.ComponentModel.DataAnnotations;

public enum PageElementType
{
    Photo,
    Text
}

public class PageElement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public PageElementType Type { get; set; }

    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Rotation { get; set; }
    public int ZIndex { get; set; }

    public Guid? PhotoId { get; set; }

    [StringLength(2000)]
    public string? Src { get; set; }

    [StringLength(50)]
    public string ObjectFit { get; set; } = "cover";

    [StringLength(4000)]
    public string? Text { get; set; }

    [Range(1, 400)]
    public int FontSize { get; set; } = 16;

    [StringLength(100)]
    public string FontFamily { get; set; } = "Arial";

    [StringLength(20)]
    public string FontWeight { get; set; } = "400";

    [StringLength(20)]
    public string Color { get; set; } = "#000000";

    [StringLength(20)]
    public string TextAlign { get; set; } = "center";
}
