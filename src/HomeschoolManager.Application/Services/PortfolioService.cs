using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IPortfolioFileStorage _fileStorage;

    public PortfolioService(
        IPortfolioRepository portfolioRepository,
        IPortfolioFileStorage fileStorage)
    {
        _portfolioRepository = portfolioRepository;
        _fileStorage = fileStorage;
    }

    public async Task<PortfolioItem?> GetItemByIdAsync(int id)
    {
        return await _portfolioRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<PortfolioItem>> GetItemsAsync(PortfolioFilter filter)
    {
        return await _portfolioRepository.GetFilteredAsync(filter);
    }

    public async Task<PortfolioItem> CreateItemAsync(PortfolioItem item, PortfolioUpload? upload = null)
    {
        Normalize(item);
        if (upload is not null)
        {
            ApplyStoredFile(item, await _fileStorage.SaveAsync(upload.Stream, upload.FileName, upload.ContentType));
        }

        item.CreatedAt = DateTime.UtcNow;
        return await _portfolioRepository.AddAsync(item);
    }

    public async Task<PortfolioItem> UpdateItemAsync(PortfolioItem item, PortfolioUpload? upload = null)
    {
        Normalize(item);
        var existing = await _portfolioRepository.GetByIdAsync(item.Id)
            ?? throw new InvalidOperationException($"Portfolio item {item.Id} was not found.");

        if (upload is not null)
        {
            await _fileStorage.DeleteAsync(existing.StoredFilePath);
            ApplyStoredFile(item, await _fileStorage.SaveAsync(upload.Stream, upload.FileName, upload.ContentType));
        }
        else
        {
            item.OriginalFileName = existing.OriginalFileName;
            item.StoredFileName = existing.StoredFileName;
            item.StoredFilePath = existing.StoredFilePath;
            item.ContentType = existing.ContentType;
            item.FileSizeBytes = existing.FileSizeBytes;
        }

        item.CreatedAt = existing.CreatedAt;
        item.UpdatedAt = DateTime.UtcNow;
        await _portfolioRepository.UpdateAsync(item);
        return item;
    }

    public async Task DeleteItemAsync(int id)
    {
        var existing = await _portfolioRepository.GetByIdAsync(id);
        if (existing is not null)
        {
            await _fileStorage.DeleteAsync(existing.StoredFilePath);
        }

        await _portfolioRepository.DeleteAsync(id);
    }

    private static void Normalize(PortfolioItem item)
    {
        item.Date = item.Date.Date;
        item.Title = item.Title?.Trim() ?? string.Empty;
        item.Description = item.Description?.Trim() ?? string.Empty;
        item.Notes = item.Notes?.Trim() ?? string.Empty;
        item.ExternalUrl = item.ExternalUrl?.Trim() ?? string.Empty;
        item.Tags = item.Tags?.Trim() ?? string.Empty;

        if ((item.Type is PortfolioItemType.Link or PortfolioItemType.Video) && string.IsNullOrWhiteSpace(item.ExternalUrl))
        {
            throw new InvalidOperationException("A URL is required for link and video portfolio items.");
        }

        if (string.IsNullOrWhiteSpace(item.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }
    }

    private static void ApplyStoredFile(PortfolioItem item, StoredPortfolioFile storedFile)
    {
        item.OriginalFileName = storedFile.OriginalFileName;
        item.StoredFileName = storedFile.StoredFileName;
        item.StoredFilePath = storedFile.StoredFilePath;
        item.ContentType = storedFile.ContentType;
        item.FileSizeBytes = storedFile.SizeBytes;
    }
}
