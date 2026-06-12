using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonPortfolioRepository : JsonRepositoryBase<PortfolioItem>, IPortfolioRepository
{
    public JsonPortfolioRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<PortfolioItem> Items(HomeschoolData data) => data.PortfolioItems;

    protected override string EntityLabel => "Portfolio item";

    private protected override PortfolioItem Hydrate(HomeschoolData data, PortfolioItem entity) =>
        RepositoryProjection.HydratePortfolioItem(data, entity);

    protected override PortfolioItem Normalize(PortfolioItem entity)
    {
        entity.Date = entity.Date.Date;
        entity.Title = entity.Title?.Trim() ?? string.Empty;
        entity.Description = entity.Description?.Trim() ?? string.Empty;
        entity.Notes = entity.Notes?.Trim() ?? string.Empty;
        entity.Subject = entity.Subject?.Trim() ?? string.Empty;
        entity.ExternalUrl = entity.ExternalUrl?.Trim() ?? string.Empty;
        entity.Tags = entity.Tags?.Trim() ?? string.Empty;
        return entity;
    }

    private protected override IEnumerable<PortfolioItem> Order(HomeschoolData data, IEnumerable<PortfolioItem> items) =>
        items.OrderByDescending(i => i.Date).ThenBy(i => i.Title);

    private protected override void Validate(HomeschoolData data, PortfolioItem entity)
    {
        if (entity.StudentId <= 0 || !data.Students.Any(s => s.Id == entity.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        if (entity.SubjectId <= 0 || !data.Courses.Any(c => c.Id == entity.SubjectId))
        {
            throw new InvalidOperationException("A valid subject is required.");
        }

        if (entity.AssignmentId.HasValue && !data.Assignments.Any(a => a.Id == entity.AssignmentId.Value))
        {
            throw new InvalidOperationException("The linked assignment was not found.");
        }

        if (entity.LessonPlanId.HasValue && !data.LessonPlans.Any(lp => lp.Id == entity.LessonPlanId.Value))
        {
            throw new InvalidOperationException("The linked lesson plan was not found.");
        }
    }

    private protected override void OnSaving(HomeschoolData data, PortfolioItem entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.Subject))
        {
            return;
        }

        var course = data.Courses.FirstOrDefault(c => c.Id == entity.SubjectId);
        entity.Subject = course == null ? string.Empty : string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject;
    }

    public override async Task<IEnumerable<PortfolioItem>> GetAllAsync()
    {
        return await GetFilteredAsync(new PortfolioFilter());
    }

    public async Task<IEnumerable<PortfolioItem>> GetFilteredAsync(PortfolioFilter filter)
    {
        var data = await Store.ReadAsync();
        var query = data.PortfolioItems.AsEnumerable();

        if (filter.StudentId.HasValue && filter.StudentId.Value > 0)
        {
            query = query.Where(i => i.StudentId == filter.StudentId.Value);
        }

        if (filter.SubjectId.HasValue && filter.SubjectId.Value > 0)
        {
            query = query.Where(i => i.SubjectId == filter.SubjectId.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(i => i.Type == filter.Type.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(i => i.Date.Date >= filter.StartDate.Value.Date);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(i => i.Date.Date <= filter.EndDate.Value.Date);
        }

        if (filter.BestWorkOnly)
        {
            query = query.Where(i => i.IsBestWork);
        }

        return query
            .OrderByDescending(i => i.Date)
            .ThenBy(i => i.Title)
            .Select(i => RepositoryProjection.HydratePortfolioItem(data, i))
            .ToList();
    }

    public async Task<IEnumerable<PortfolioItem>> GetByStudentIdAsync(int studentId)
    {
        return await GetFilteredAsync(new PortfolioFilter(StudentId: studentId));
    }

    public async Task<IEnumerable<PortfolioItem>> GetByAssignmentIdAsync(int assignmentId)
    {
        var data = await Store.ReadAsync();
        return data.PortfolioItems
            .Where(i => i.AssignmentId == assignmentId)
            .OrderByDescending(i => i.Date)
            .Select(i => RepositoryProjection.HydratePortfolioItem(data, i))
            .ToList();
    }

    public async Task<IEnumerable<PortfolioItem>> GetByLessonPlanIdAsync(int lessonPlanId)
    {
        var data = await Store.ReadAsync();
        return data.PortfolioItems
            .Where(i => i.LessonPlanId == lessonPlanId)
            .OrderByDescending(i => i.Date)
            .Select(i => RepositoryProjection.HydratePortfolioItem(data, i))
            .ToList();
    }
}
