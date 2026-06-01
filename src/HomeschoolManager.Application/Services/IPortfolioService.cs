using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public interface IPortfolioService
{
    Task<PortfolioItem?> GetItemByIdAsync(int id);
    Task<IEnumerable<PortfolioItem>> GetItemsAsync(PortfolioFilter filter);
    Task<PortfolioItem> CreateItemAsync(PortfolioItem item, PortfolioUpload? upload = null);
    Task<PortfolioItem> UpdateItemAsync(PortfolioItem item, PortfolioUpload? upload = null);
    Task DeleteItemAsync(int id);
}

public sealed record PortfolioUpload(
    Stream Stream,
    string FileName,
    string ContentType);
