using System.Text.Json;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonYearbookRepository : JsonRepositoryBase<Yearbook>, IYearbookRepository
{
    public JsonYearbookRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<Yearbook> Items(HomeschoolData data) => data.Yearbooks;

    protected override string EntityLabel => "Yearbook";

    private protected override Yearbook Hydrate(HomeschoolData data, Yearbook entity) =>
        RepositoryProjection.HydrateYearbook(data, entity);

    protected override Yearbook Normalize(Yearbook entity)
    {
        entity.Title = entity.Title?.Trim() ?? string.Empty;
        entity.SchoolYear = entity.SchoolYear?.Trim() ?? string.Empty;
        entity.StartDate = entity.StartDate.Date;
        entity.EndDate = entity.EndDate.Date;
        return entity;
    }

    private protected override IEnumerable<Yearbook> Order(HomeschoolData data, IEnumerable<Yearbook> items) =>
        items.OrderByDescending(y => y.StartDate).ThenBy(y => y.Title);

    private protected override void Validate(HomeschoolData data, Yearbook entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(entity.SchoolYear))
        {
            throw new InvalidOperationException("School year is required.");
        }

        if (entity.EndDate < entity.StartDate)
        {
            throw new InvalidOperationException("Start date must be before or equal to end date.");
        }

        if (entity.Scope == YearbookScope.Student && (!entity.StudentId.HasValue || !data.Students.Any(s => s.Id == entity.StudentId.Value)))
        {
            throw new InvalidOperationException("Student is required for student yearbooks.");
        }
    }

    private protected override void OnDeleting(HomeschoolData data, int id)
    {
        var pageIds = data.YearbookPages.Where(p => p.YearbookId == id).Select(p => p.Id).ToHashSet();
        data.YearbookPages.RemoveAll(p => p.YearbookId == id);
        data.YearbookAssets.RemoveAll(a => a.YearbookId == id || (a.YearbookPageId.HasValue && pageIds.Contains(a.YearbookPageId.Value)));
    }

    public async Task<IEnumerable<Yearbook>> GetByFamilyIdAsync(int familyId)
    {
        var data = await Store.ReadAsync();
        return data.Yearbooks
            .Where(y => y.FamilyId == familyId)
            .OrderByDescending(y => y.StartDate)
            .ThenBy(y => y.Title)
            .Select(y => RepositoryProjection.HydrateYearbook(data, y))
            .ToList();
    }

    public async Task<IEnumerable<YearbookPage>> GetPagesAsync(int yearbookId)
    {
        var data = await Store.ReadAsync();
        return data.YearbookPages
            .Where(p => p.YearbookId == yearbookId)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .Select(p => RepositoryProjection.HydrateYearbookPage(data, p))
            .ToList();
    }

    public async Task<YearbookPage?> GetPageByIdAsync(int pageId)
    {
        var data = await Store.ReadAsync();
        var page = data.YearbookPages.FirstOrDefault(p => p.Id == pageId);
        return page == null ? null : RepositoryProjection.HydrateYearbookPage(data, page);
    }

    public async Task<YearbookPage> AddPageAsync(YearbookPage page)
    {
        var saved = NormalizePage(HomeschoolDataStore.Clone(page));
        await Store.WriteAsync(data =>
        {
            ValidatePage(data, saved);
            saved.Id = saved.Id == 0 ? NextId(data.YearbookPages.Select(p => p.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            data.YearbookPages.Add(saved);
        });

        return await GetPageByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdatePageAsync(YearbookPage page)
    {
        var updated = NormalizePage(HomeschoolDataStore.Clone(page));
        await Store.WriteAsync(data =>
        {
            var index = data.YearbookPages.FindIndex(p => p.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Yearbook page {updated.Id} was not found.");
            }

            ValidatePage(data, updated);
            updated.CreatedAt = updated.CreatedAt == default ? data.YearbookPages[index].CreatedAt : updated.CreatedAt;
            data.YearbookPages[index] = updated;
        });
    }

    public async Task DeletePageAsync(int pageId)
    {
        await Store.WriteAsync(data =>
        {
            data.YearbookPages.RemoveAll(p => p.Id == pageId);
            data.YearbookAssets.RemoveAll(a => a.YearbookPageId == pageId);
        });
    }

    public async Task SavePagesAsync(int yearbookId, IEnumerable<YearbookPage> pages)
    {
        var updatedPages = pages.Select(p => NormalizePage(HomeschoolDataStore.Clone(p))).ToList();
        await Store.WriteAsync(data =>
        {
            if (!data.Yearbooks.Any(y => y.Id == yearbookId))
            {
                throw new InvalidOperationException($"Yearbook {yearbookId} was not found.");
            }

            foreach (var page in updatedPages)
            {
                page.YearbookId = yearbookId;
                ValidatePage(data, page);
            }

            data.YearbookPages.RemoveAll(p => p.YearbookId == yearbookId);
            var nextId = NextId(data.YearbookPages.Select(p => p.Id).Concat(updatedPages.Select(p => p.Id)));
            foreach (var page in updatedPages.OrderBy(p => p.SortOrder))
            {
                if (page.Id == 0)
                {
                    page.Id = nextId++;
                    page.CreatedAt = DateTime.UtcNow;
                }

                data.YearbookPages.Add(page);
            }
        });
    }

    public async Task<IEnumerable<YearbookAsset>> GetAssetsAsync(int yearbookId)
    {
        var data = await Store.ReadAsync();
        return data.YearbookAssets
            .Where(a => a.YearbookId == yearbookId)
            .OrderBy(a => a.Title)
            .Select(a => RepositoryProjection.HydrateYearbookAsset(data, a))
            .ToList();
    }

    public async Task SaveAssetsAsync(int yearbookId, IEnumerable<YearbookAsset> assets)
    {
        var updatedAssets = assets.Select(a => NormalizeAsset(HomeschoolDataStore.Clone(a))).ToList();
        await Store.WriteAsync(data =>
        {
            if (!data.Yearbooks.Any(y => y.Id == yearbookId))
            {
                throw new InvalidOperationException($"Yearbook {yearbookId} was not found.");
            }

            foreach (var asset in updatedAssets)
            {
                asset.YearbookId = yearbookId;
                ValidateAsset(data, asset);
            }

            data.YearbookAssets.RemoveAll(a => a.YearbookId == yearbookId);
            var nextId = NextId(data.YearbookAssets.Select(a => a.Id).Concat(updatedAssets.Select(a => a.Id)));
            foreach (var asset in updatedAssets)
            {
                if (asset.Id == 0)
                {
                    asset.Id = nextId++;
                    asset.CreatedAt = DateTime.UtcNow;
                }

                data.YearbookAssets.Add(asset);
            }
        });
    }

    private static YearbookPage NormalizePage(YearbookPage page)
    {
        page.Title = page.Title?.Trim() ?? string.Empty;
        page.ContentJson = string.IsNullOrWhiteSpace(page.ContentJson) ? "{}" : page.ContentJson.Trim();
        YearbookPageMigration.EnsureElements(page);
        return page;
    }

    private static YearbookAsset NormalizeAsset(YearbookAsset asset)
    {
        asset.Title = asset.Title?.Trim() ?? string.Empty;
        asset.SourcePath = asset.SourcePath?.Trim() ?? string.Empty;
        asset.Caption = asset.Caption?.Trim() ?? string.Empty;
        return asset;
    }

    private static void ValidatePage(HomeschoolData data, YearbookPage page)
    {
        if (!data.Yearbooks.Any(y => y.Id == page.YearbookId))
        {
            throw new InvalidOperationException("A valid yearbook is required.");
        }

        if (string.IsNullOrWhiteSpace(page.Title))
        {
            throw new InvalidOperationException("Page title is required.");
        }

        if (page.SortOrder < 0)
        {
            throw new InvalidOperationException("Page sort order must be non-negative.");
        }

        try
        {
            using var _ = JsonDocument.Parse(page.ContentJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Page content must be valid JSON.", ex);
        }
    }

    private static void ValidateAsset(HomeschoolData data, YearbookAsset asset)
    {
        if (!data.Yearbooks.Any(y => y.Id == asset.YearbookId))
        {
            throw new InvalidOperationException("A valid yearbook is required.");
        }

        if (asset.YearbookPageId.HasValue && !data.YearbookPages.Any(p => p.Id == asset.YearbookPageId.Value))
        {
            throw new InvalidOperationException("The linked yearbook page was not found.");
        }

        if (asset.PortfolioItemId.HasValue && !data.PortfolioItems.Any(i => i.Id == asset.PortfolioItemId.Value))
        {
            throw new InvalidOperationException("The linked portfolio item was not found.");
        }

        if (string.IsNullOrWhiteSpace(asset.Title))
        {
            throw new InvalidOperationException("Asset title is required.");
        }
    }
}
