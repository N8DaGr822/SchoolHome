using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface IYearbookRepository : IRepository<Yearbook>
{
    Task<IEnumerable<Yearbook>> GetByFamilyIdAsync(int familyId);
    Task<IEnumerable<YearbookPage>> GetPagesAsync(int yearbookId);
    Task<YearbookPage?> GetPageByIdAsync(int pageId);
    Task<YearbookPage> AddPageAsync(YearbookPage page);
    Task UpdatePageAsync(YearbookPage page);
    Task DeletePageAsync(int pageId);
    Task SavePagesAsync(int yearbookId, IEnumerable<YearbookPage> pages);
    Task<IEnumerable<YearbookAsset>> GetAssetsAsync(int yearbookId);
    Task SaveAssetsAsync(int yearbookId, IEnumerable<YearbookAsset> assets);
}
