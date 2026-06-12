namespace HomeschoolManager.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Returns a page of records. The default implementation pages in memory over
    /// <see cref="GetAllAsync"/>; implementations can override to page at the source.
    /// </summary>
    async Task<IReadOnlyList<T>> GetPagedAsync(int skip, int take)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be non-negative.");
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be positive.");
        }

        var all = await GetAllAsync();
        return all.Skip(skip).Take(take).ToList();
    }
}
