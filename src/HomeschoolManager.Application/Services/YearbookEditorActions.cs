using System.Security.Cryptography;
using System.Text;
using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public static class YearbookEditorActions
{
    public static PageElement AddTextBox(YearbookPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        page.Elements ??= new List<PageElement>();

        var element = new PageElement
        {
            Type = PageElementType.Text,
            X = 100,
            Y = 100,
            Width = 180,
            Height = 40,
            Text = "Student Name",
            FontSize = 16,
            FontWeight = "600",
            TextAlign = "center",
            ZIndex = GetNextZIndex(page)
        };

        page.Elements.Add(element);
        return element;
    }

    public static PageElement AddPhoto(YearbookPage page, PortfolioItem? photo)
    {
        ArgumentNullException.ThrowIfNull(page);
        page.Elements ??= new List<PageElement>();

        var element = new PageElement
        {
            Type = PageElementType.Photo,
            X = 100,
            Y = 100,
            Width = 160,
            Height = 200,
            PhotoId = photo is null ? null : GetPortfolioPhotoId(photo.Id),
            Src = GetPhotoSource(photo),
            ObjectFit = "cover",
            ZIndex = GetNextZIndex(page)
        };

        page.Elements.Add(element);
        return element;
    }

    public static PageElement? DuplicateSelectedElement(YearbookPage page, Guid? selectedElementId)
    {
        ArgumentNullException.ThrowIfNull(page);
        page.Elements ??= new List<PageElement>();

        var selected = page.Elements.FirstOrDefault(e => e.Id == selectedElementId);
        if (selected is null)
        {
            return null;
        }

        var duplicate = new PageElement
        {
            Id = Guid.NewGuid(),
            Type = selected.Type,
            X = selected.X + 20,
            Y = selected.Y + 20,
            Width = selected.Width,
            Height = selected.Height,
            Rotation = selected.Rotation,
            ZIndex = GetNextZIndex(page),
            PhotoId = selected.PhotoId,
            Src = selected.Src,
            ObjectFit = selected.ObjectFit,
            Text = selected.Text,
            FontSize = selected.FontSize,
            FontFamily = selected.FontFamily,
            FontWeight = selected.FontWeight,
            Color = selected.Color,
            TextAlign = selected.TextAlign
        };

        page.Elements.Add(duplicate);
        return duplicate;
    }

    public static PageElement? DeleteSelectedElement(YearbookPage page, Guid? selectedElementId)
    {
        ArgumentNullException.ThrowIfNull(page);
        page.Elements ??= new List<PageElement>();

        var selected = page.Elements.FirstOrDefault(e => e.Id == selectedElementId);
        if (selected is null)
        {
            return null;
        }

        page.Elements.Remove(selected);
        return selected;
    }

    public static int GetNextZIndex(YearbookPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        page.Elements ??= new List<PageElement>();

        return page.Elements.Count == 0 ? 0 : page.Elements.Max(e => e.ZIndex) + 1;
    }

    public static Guid GetPortfolioPhotoId(int portfolioItemId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"portfolio-item:{portfolioItemId}"));
        return new Guid(bytes[..16]);
    }

    public static string? GetPhotoSource(PortfolioItem? photo)
    {
        if (photo is null)
        {
            return null;
        }

        return !string.IsNullOrWhiteSpace(photo.StoredFilePath)
            ? photo.StoredFilePath
            : string.IsNullOrWhiteSpace(photo.ExternalUrl) ? null : photo.ExternalUrl;
    }
}
