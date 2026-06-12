using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonCurriculumResourceRepository : JsonRepositoryBase<CurriculumResource>, ICurriculumResourceRepository
{
    public JsonCurriculumResourceRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<CurriculumResource> Items(HomeschoolData data) => data.CurriculumResources;

    protected override string EntityLabel => "Curriculum resource";

    private protected override CurriculumResource Hydrate(HomeschoolData data, CurriculumResource entity) =>
        RepositoryProjection.HydrateCurriculumResource(data, entity);

    protected override CurriculumResource Normalize(CurriculumResource entity)
    {
        entity.Title = entity.Title?.Trim() ?? string.Empty;
        entity.Description = entity.Description?.Trim() ?? string.Empty;
        entity.Subject = entity.Subject?.Trim() ?? string.Empty;
        entity.Publisher = entity.Publisher?.Trim() ?? string.Empty;
        entity.Author = entity.Author?.Trim() ?? string.Empty;
        entity.Url = entity.Url?.Trim() ?? string.Empty;
        entity.GradeLevel = entity.GradeLevel?.Trim() ?? string.Empty;
        return entity;
    }

    private protected override void Validate(HomeschoolData data, CurriculumResource entity)
    {
        if (entity.SubjectId <= 0 || !data.Courses.Any(c => c.Id == entity.SubjectId))
        {
            throw new InvalidOperationException("A valid subject is required.");
        }
    }

    private protected override void OnSaving(HomeschoolData data, CurriculumResource entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.Subject))
        {
            return;
        }

        var course = data.Courses.FirstOrDefault(c => c.Id == entity.SubjectId);
        entity.Subject = course == null ? string.Empty : string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject;
    }

    private protected override void OnDeleting(HomeschoolData data, int id)
    {
        data.StudentCurricula.RemoveAll(c => c.CurriculumResourceId == id);
    }

    public override async Task<IEnumerable<CurriculumResource>> GetAllAsync()
    {
        return await GetFilteredAsync(new CurriculumResourceFilter());
    }

    public async Task<IEnumerable<CurriculumResource>> GetFilteredAsync(CurriculumResourceFilter filter)
    {
        var data = await Store.ReadAsync();
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
}
