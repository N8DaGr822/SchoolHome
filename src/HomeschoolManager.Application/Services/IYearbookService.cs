using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public interface IYearbookService
{
    Task<IEnumerable<Yearbook>> GetYearbooksAsync(int familyId = 1);
    Task<Yearbook?> GetYearbookByIdAsync(int id);
    Task<Yearbook> CreateYearbookAsync(Yearbook yearbook);
    Task<Yearbook> UpdateYearbookAsync(Yearbook yearbook);
    Task DeleteYearbookAsync(int id);
    Task<YearbookPage> AddCustomPageAsync(int yearbookId);
    Task UpdatePageAsync(YearbookPage page);
    Task DeletePageAsync(int pageId);
    Task MovePageAsync(int yearbookId, int pageId, int direction);
    Task SavePagesAsync(int yearbookId, IEnumerable<YearbookPage> pages);
    Task<IEnumerable<PortfolioItem>> GetPortfolioCandidatesAsync(int yearbookId);
    Task SavePortfolioSelectionsAsync(int yearbookId, IReadOnlyDictionary<int, IEnumerable<int>> pagePortfolioItemIds);
}
