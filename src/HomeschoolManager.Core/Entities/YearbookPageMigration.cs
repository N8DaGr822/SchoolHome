namespace HomeschoolManager.Core.Entities;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static class YearbookPageMigration
{
    private const double PageMargin = 72;
    private const double TextY = 96;
    private const double TextWidth = 672;
    private const double TextHeight = 160;
    private const double PhotoStartY = 300;
    private const double PhotoWidth = 300;
    private const double PhotoHeight = 200;
    private const double PhotoGap = 24;

    public static void EnsureElements(YearbookPage page)
    {
        EnsureElements(page, Enumerable.Empty<YearbookAsset>());
    }

    public static void EnsureElements(YearbookPage page, IEnumerable<YearbookAsset> assets)
    {
        page.Elements ??= new List<PageElement>();

        AddTextElement(page);
        AddPhotoElements(page, assets);
    }

    private static void AddTextElement(YearbookPage page)
    {
        var text = ExtractLegacyText(page.ContentJson);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (page.Elements.Any(e => e.Type == PageElementType.Text))
        {
            return;
        }

        var elementId = CreateStableId(page, "text", "body");
        if (page.Elements.Any(e => e.Id == elementId))
        {
            return;
        }

        page.Elements.Add(new PageElement
        {
            Id = elementId,
            Type = PageElementType.Text,
            X = PageMargin,
            Y = TextY,
            Width = TextWidth,
            Height = TextHeight,
            ZIndex = NextZIndex(page),
            Text = text,
            FontSize = 18,
            FontFamily = "Arial",
            FontWeight = "400",
            Color = "#000000",
            TextAlign = "center"
        });
    }

    private static void AddPhotoElements(YearbookPage page, IEnumerable<YearbookAsset> assets)
    {
        var pageAssets = assets
            .Where(asset => asset.YearbookPageId == page.Id)
            .OrderBy(asset => asset.Id)
            .ThenBy(asset => asset.Title)
            .ToList();

        for (var index = 0; index < pageAssets.Count; index++)
        {
            var asset = pageAssets[index];
            var elementId = CreateStableId(page, "photo", asset.Id.ToString());
            if (page.Elements.Any(e => e.Id == elementId))
            {
                continue;
            }

            var source = !string.IsNullOrWhiteSpace(asset.SourcePath)
                ? asset.SourcePath
                : asset.PortfolioItem?.StoredFilePath ?? asset.PortfolioItem?.ExternalUrl;
            var photoId = CreateStableId(page, "photo-source", asset.PortfolioItemId?.ToString() ?? asset.Id.ToString());
            if (page.Elements.Any(e =>
                    e.Type == PageElementType.Photo
                    && (e.Id == elementId
                        || e.PhotoId == photoId
                        || (!string.IsNullOrWhiteSpace(source) && string.Equals(e.Src, source, StringComparison.OrdinalIgnoreCase)))))
            {
                continue;
            }

            var column = index % 2;
            var row = index / 2;

            page.Elements.Add(new PageElement
            {
                Id = elementId,
                Type = PageElementType.Photo,
                X = PageMargin + column * (PhotoWidth + PhotoGap),
                Y = PhotoStartY + row * (PhotoHeight + PhotoGap),
                Width = PhotoWidth,
                Height = PhotoHeight,
                ZIndex = NextZIndex(page),
                PhotoId = photoId,
                Src = string.IsNullOrWhiteSpace(source) ? null : source,
                ObjectFit = "cover"
            });
        }
    }

    private static string? ExtractLegacyText(string? contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson) || contentJson.Trim() == "{}")
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(contentJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (document.RootElement.TryGetProperty("body", out var body))
                {
                    return body.GetString();
                }

                if (document.RootElement.TryGetProperty("text", out var text))
                {
                    return text.GetString();
                }
            }

            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                return document.RootElement.GetString();
            }
        }
        catch (JsonException)
        {
            return contentJson;
        }

        return null;
    }

    private static int NextZIndex(YearbookPage page)
    {
        return page.Elements.Count == 0 ? 0 : page.Elements.Max(e => e.ZIndex) + 1;
    }

    private static Guid CreateStableId(YearbookPage page, string kind, string sourceId)
    {
        var key = $"{page.YearbookId}:{page.SortOrder}:{page.Title}:{kind}:{sourceId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash[..16]);
    }
}
