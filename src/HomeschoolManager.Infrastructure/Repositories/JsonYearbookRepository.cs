using System.Text.Json;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonYearbookRepository : IYearbookRepository
{
    private readonly HomeschoolDataStore _store;

    public JsonYearbookRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<Yearbook?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var yearbook = data.Yearbooks.FirstOrDefault(y => y.Id == id);
        return yearbook == null ? null : RepositoryProjection.HydrateYearbook(data, yearbook);
    }

    public async Task<IEnumerable<Yearbook>> GetAllAsync()
    {
        var data = await _store.ReadAsync();
        return data.Yearbooks
            .OrderByDescending(y => y.StartDate)
            .ThenBy(y => y.Title)
            .Select(y => RepositoryProjection.HydrateYearbook(data, y))
            .ToList();
    }

    public async Task<IEnumerable<Yearbook>> GetByFamilyIdAsync(int familyId)
    {
        var data = await _store.ReadAsync();
        return data.Yearbooks
            .Where(y => y.FamilyId == familyId)
            .OrderByDescending(y => y.StartDate)
            .ThenBy(y => y.Title)
            .Select(y => RepositoryProjection.HydrateYearbook(data, y))
            .ToList();
    }

    public async Task<Yearbook> AddAsync(Yearbook entity)
    {
        var saved = NormalizeYearbook(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            ValidateYearbook(data, saved);
            saved.Id = saved.Id == 0 ? NextId(data.Yearbooks.Select(y => y.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            data.Yearbooks.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(Yearbook entity)
    {
        var updated = NormalizeYearbook(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            var index = data.Yearbooks.FindIndex(y => y.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Yearbook {updated.Id} was not found.");
            }

            ValidateYearbook(data, updated);
            updated.CreatedAt = updated.CreatedAt == default ? data.Yearbooks[index].CreatedAt : updated.CreatedAt;
            data.Yearbooks[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data =>
        {
            data.Yearbooks.RemoveAll(y => y.Id == id);
            var pageIds = data.YearbookPages.Where(p => p.YearbookId == id).Select(p => p.Id).ToHashSet();
            data.YearbookPages.RemoveAll(p => p.YearbookId == id);
            data.YearbookAssets.RemoveAll(a => a.YearbookId == id || (a.YearbookPageId.HasValue && pageIds.Contains(a.YearbookPageId.Value)));
        });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.Yearbooks.Any(y => y.Id == id);
    }

    public async Task<IEnumerable<YearbookPage>> GetPagesAsync(int yearbookId)
    {
        var data = await _store.ReadAsync();
        return data.YearbookPages
            .Where(p => p.YearbookId == yearbookId)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .Select(p => RepositoryProjection.HydrateYearbookPage(data, p))
            .ToList();
    }

    public async Task<YearbookPage?> GetPageByIdAsync(int pageId)
    {
        var data = await _store.ReadAsync();
        var page = data.YearbookPages.FirstOrDefault(p => p.Id == pageId);
        return page == null ? null : RepositoryProjection.HydrateYearbookPage(data, page);
    }

    public async Task<YearbookPage> AddPageAsync(YearbookPage page)
    {
        var saved = NormalizePage(HomeschoolDataStore.Clone(page));
        await _store.WriteAsync(data =>
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
        await _store.WriteAsync(data =>
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
        await _store.WriteAsync(data =>
        {
            data.YearbookPages.RemoveAll(p => p.Id == pageId);
            data.YearbookAssets.RemoveAll(a => a.YearbookPageId == pageId);
        });
    }

    public async Task SavePagesAsync(int yearbookId, IEnumerable<YearbookPage> pages)
    {
        var updatedPages = pages.Select(p => NormalizePage(HomeschoolDataStore.Clone(p))).ToList();
        await _store.WriteAsync(data =>
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
        var data = await _store.ReadAsync();
        return data.YearbookAssets
            .Where(a => a.YearbookId == yearbookId)
            .OrderBy(a => a.Title)
            .Select(a => RepositoryProjection.HydrateYearbookAsset(data, a))
            .ToList();
    }

    public async Task SaveAssetsAsync(int yearbookId, IEnumerable<YearbookAsset> assets)
    {
        var updatedAssets = assets.Select(a => NormalizeAsset(HomeschoolDataStore.Clone(a))).ToList();
        await _store.WriteAsync(data =>
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

    private static Yearbook NormalizeYearbook(Yearbook yearbook)
    {
        yearbook.Title = yearbook.Title?.Trim() ?? string.Empty;
        yearbook.SchoolYear = yearbook.SchoolYear?.Trim() ?? string.Empty;
        yearbook.StartDate = yearbook.StartDate.Date;
        yearbook.EndDate = yearbook.EndDate.Date;
        return yearbook;
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

    private static void ValidateYearbook(HomeschoolData data, Yearbook yearbook)
    {
        if (string.IsNullOrWhiteSpace(yearbook.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(yearbook.SchoolYear))
        {
            throw new InvalidOperationException("School year is required.");
        }

        if (yearbook.EndDate < yearbook.StartDate)
        {
            throw new InvalidOperationException("Start date must be before or equal to end date.");
        }

        if (yearbook.Scope == YearbookScope.Student && (!yearbook.StudentId.HasValue || !data.Students.Any(s => s.Id == yearbook.StudentId.Value)))
        {
            throw new InvalidOperationException("Student is required for student yearbooks.");
        }
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

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
