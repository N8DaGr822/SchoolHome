using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class YearbookEditorActionsTests
{
    [Fact]
    public void AddTextBox_CreatesDefaultTextElementWithNextZIndex()
    {
        var page = new YearbookPage
        {
            Elements =
            [
                new PageElement { Type = PageElementType.Text, ZIndex = 3 }
            ]
        };

        var element = YearbookEditorActions.AddTextBox(page);

        Assert.Equal(PageElementType.Text, element.Type);
        Assert.Equal(100, element.X);
        Assert.Equal(100, element.Y);
        Assert.Equal(180, element.Width);
        Assert.Equal(40, element.Height);
        Assert.Equal("Student Name", element.Text);
        Assert.Equal(16, element.FontSize);
        Assert.Equal("600", element.FontWeight);
        Assert.Equal("center", element.TextAlign);
        Assert.Equal(4, element.ZIndex);
        Assert.Contains(element, page.Elements);
    }

    [Fact]
    public void AddPhoto_UsesPortfolioSourceAndDefaultPhotoLayout()
    {
        var page = new YearbookPage();
        var photo = new PortfolioItem
        {
            Id = 12,
            Type = PortfolioItemType.Photo,
            Title = "Portrait",
            ExternalUrl = "https://example.com/portrait.jpg"
        };

        var element = YearbookEditorActions.AddPhoto(page, photo);

        Assert.Equal(PageElementType.Photo, element.Type);
        Assert.Equal(100, element.X);
        Assert.Equal(100, element.Y);
        Assert.Equal(160, element.Width);
        Assert.Equal(200, element.Height);
        Assert.Equal(YearbookEditorActions.GetPortfolioPhotoId(photo.Id), element.PhotoId);
        Assert.Equal(photo.ExternalUrl, element.Src);
        Assert.Equal("cover", element.ObjectFit);
        Assert.Equal(0, element.ZIndex);
    }

    [Fact]
    public void DuplicateSelectedElement_ClonesSelectedElementWithNewIdentityOffsetAndZIndex()
    {
        var selected = new PageElement
        {
            Type = PageElementType.Text,
            X = 10,
            Y = 20,
            Width = 180,
            Height = 40,
            ZIndex = 2,
            Text = "Original",
            FontWeight = "600"
        };
        var page = new YearbookPage
        {
            Elements =
            [
                selected,
                new PageElement { Type = PageElementType.Photo, ZIndex = 5 }
            ]
        };

        var duplicate = YearbookEditorActions.DuplicateSelectedElement(page, selected.Id);

        Assert.NotNull(duplicate);
        Assert.NotEqual(selected.Id, duplicate!.Id);
        Assert.Equal(30, duplicate.X);
        Assert.Equal(40, duplicate.Y);
        Assert.Equal(6, duplicate.ZIndex);
        Assert.Equal(selected.Text, duplicate.Text);
        Assert.Equal(selected.FontWeight, duplicate.FontWeight);
        Assert.Contains(duplicate, page.Elements);
    }

    [Fact]
    public void DeleteSelectedElement_RemovesAndReturnsSelectedElement()
    {
        var selected = new PageElement { Type = PageElementType.Photo };
        var page = new YearbookPage
        {
            Elements =
            [
                selected,
                new PageElement { Type = PageElementType.Text }
            ]
        };

        var deleted = YearbookEditorActions.DeleteSelectedElement(page, selected.Id);

        Assert.Equal(selected, deleted);
        Assert.DoesNotContain(selected, page.Elements);
        Assert.Single(page.Elements);
    }
}
