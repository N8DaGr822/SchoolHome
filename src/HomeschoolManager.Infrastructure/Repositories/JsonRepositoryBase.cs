using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

/// <summary>
/// Shared CRUD pipeline for the JSON-backed repositories. Derived classes plug in
/// the entity collection, ordering, hydration, normalization, validation, and
/// cascade behavior through the template hooks below.
/// </summary>
public abstract class JsonRepositoryBase<T> : IRepository<T> where T : class, IEntity
{
    protected JsonRepositoryBase(HomeschoolDataStore store)
    {
        Store = store;
    }

    protected HomeschoolDataStore Store { get; }

    /// <summary>The collection inside <see cref="HomeschoolData"/> that holds this entity.</summary>
    private protected abstract List<T> Items(HomeschoolData data);

    /// <summary>Human-readable label used in "was not found" error messages.</summary>
    protected abstract string EntityLabel { get; }

    /// <summary>Attaches navigation properties for reads. Defaults to a pass-through.</summary>
    private protected virtual T Hydrate(HomeschoolData data, T entity) => entity;

    /// <summary>Trims/coerces fields before persisting. Defaults to a pass-through.</summary>
    protected virtual T Normalize(T entity) => entity;

    /// <summary>Default ordering for <see cref="GetAllAsync"/>. Defaults to storage order.</summary>
    private protected virtual IEnumerable<T> Order(HomeschoolData data, IEnumerable<T> items) => items;

    /// <summary>
    /// Validates references and duplicates before persisting. Runs after the id is
    /// assigned, so duplicate checks should exclude the entity's own id. Defaults to a no-op.
    /// </summary>
    private protected virtual void Validate(HomeschoolData data, T entity)
    {
    }

    /// <summary>Last-chance mutation (e.g. filling denormalized fields) before add/update.</summary>
    private protected virtual void OnSaving(HomeschoolData data, T entity)
    {
    }

    /// <summary>Removes dependent records when an entity is deleted. Defaults to a no-op.</summary>
    private protected virtual void OnDeleting(HomeschoolData data, int id)
    {
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        var data = await Store.ReadAsync();
        var entity = Items(data).FirstOrDefault(e => e.Id == id);
        return entity == null ? null : Hydrate(data, entity);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        var data = await Store.ReadAsync();
        return Order(data, Items(data))
            .Select(e => Hydrate(data, e))
            .ToList();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        var saved = Normalize(HomeschoolDataStore.Clone(entity));
        await Store.WriteAsync(data =>
        {
            saved.Id = saved.Id == 0 ? NextId(Items(data).Select(e => e.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            Validate(data, saved);
            OnSaving(data, saved);
            Items(data).Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        var updated = Normalize(HomeschoolDataStore.Clone(entity));
        await Store.WriteAsync(data =>
        {
            var items = Items(data);
            var index = items.FindIndex(e => e.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"{EntityLabel} {updated.Id} was not found.");
            }

            Validate(data, updated);
            updated.CreatedAt = updated.CreatedAt == default ? items[index].CreatedAt : updated.CreatedAt;
            OnSaving(data, updated);
            items[index] = updated;
        });
    }

    public virtual async Task DeleteAsync(int id)
    {
        await Store.WriteAsync(data =>
        {
            OnDeleting(data, id);
            Items(data).RemoveAll(e => e.Id == id);
        });
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        var data = await Store.ReadAsync();
        return Items(data).Any(e => e.Id == id);
    }

    public virtual async Task<IReadOnlyList<T>> GetPagedAsync(int skip, int take)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be non-negative.");
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be positive.");
        }

        var data = await Store.ReadAsync();
        return Order(data, Items(data))
            .Skip(skip)
            .Take(take)
            .Select(e => Hydrate(data, e))
            .ToList();
    }

    protected static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
