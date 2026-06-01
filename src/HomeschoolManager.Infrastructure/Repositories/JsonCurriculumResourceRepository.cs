using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonCurriculumResourceRepository : ICurriculumResourceRepository
{
    private readonly HomeschoolDataStore _store;

    public JsonCurriculumResourceRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<CurriculumResource?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var resource = data.CurriculumResources.FirstOrDefault(r => r.Id == id);
        return resource == null ? null : RepositoryProjection.HydrateCurriculumResource(data, resource);
    }

    public async Task<IEnumerable<CurriculumResource>> GetAllAsync()
    {
        return await GetFilteredAsync(new CurriculumResourceFilter());
    }

    public async Task<CurriculumResource> AddAsync(CurriculumResource entity)
    {
        var saved = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            ValidateReferences(data, saved);
            FillSubject(data, saved);
            saved.Id = saved.Id == 0 ? NextId(data.CurriculumResources.Select(r => r.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            data.CurriculumResources.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(CurriculumResource entity)
    {
        var updated = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            var index = data.CurriculumResources.FindIndex(r => r.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Curriculum resource {updated.Id} was not found.");
            }

            ValidateReferences(data, updated);
            FillSubject(data, updated);
            updated.CreatedAt = updated.CreatedAt == default ? data.CurriculumResources[index].CreatedAt : updated.CreatedAt;
            data.CurriculumResources[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data =>
        {
            data.CurriculumResources.RemoveAll(r => r.Id == id);
            data.StudentCurricula.RemoveAll(c => c.CurriculumResourceId == id);
        });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.CurriculumResources.Any(r => r.Id == id);
    }

    public async Task<IEnumerable<CurriculumResource>> GetFilteredAsync(CurriculumResourceFilter filter)
    {
        var data = await _store.ReadAsync();
        var query = data.CurriculumResources.AsEnumerable();

        if (filter.SubjectId.HasValue && filter.SubjectId.Value > 0)
        {
            query = query.Where(r => r.SubjectId == filter.SubjectId.Value);
        }

        if (filter.ResourceType.HasValue)
        {
            query = query.Where(r => r.ResourceType == filter.ResourceType.Value);
        }

        return query
            .OrderBy(r => r.Title)
            .Select(r => RepositoryProjection.HydrateCurriculumResource(data, r))
            .ToList();
    }

    private static CurriculumResource Normalize(CurriculumResource resource)
    {
        resource.Title = resource.Title?.Trim() ?? string.Empty;
        resource.Description = resource.Description?.Trim() ?? string.Empty;
        resource.Subject = resource.Subject?.Trim() ?? string.Empty;
        resource.Publisher = resource.Publisher?.Trim() ?? string.Empty;
        resource.Author = resource.Author?.Trim() ?? string.Empty;
        resource.Url = resource.Url?.Trim() ?? string.Empty;
        resource.GradeLevel = resource.GradeLevel?.Trim() ?? string.Empty;
        return resource;
    }

    private static void ValidateReferences(HomeschoolData data, CurriculumResource resource)
    {
        if (resource.SubjectId <= 0 || !data.Courses.Any(c => c.Id == resource.SubjectId))
        {
            throw new InvalidOperationException("A valid subject is required.");
        }
    }

    private static void FillSubject(HomeschoolData data, CurriculumResource resource)
    {
        if (!string.IsNullOrWhiteSpace(resource.Subject))
        {
            return;
        }

        var course = data.Courses.FirstOrDefault(c => c.Id == resource.SubjectId);
        resource.Subject = course == null ? string.Empty : string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject;
    }

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
