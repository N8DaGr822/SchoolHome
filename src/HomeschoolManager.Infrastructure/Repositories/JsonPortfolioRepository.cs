using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonPortfolioRepository : IPortfolioRepository
{
    private readonly HomeschoolDataStore _store;

    public JsonPortfolioRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<PortfolioItem?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var item = data.PortfolioItems.FirstOrDefault(i => i.Id == id);
        return item == null ? null : RepositoryProjection.HydratePortfolioItem(data, item);
    }

    public async Task<IEnumerable<PortfolioItem>> GetAllAsync()
    {
        return await GetFilteredAsync(new PortfolioFilter());
    }

    public async Task<PortfolioItem> AddAsync(PortfolioItem entity)
    {
        var saved = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            ValidateReferences(data, saved);
            FillSubject(data, saved);
            saved.Id = saved.Id == 0 ? NextId(data.PortfolioItems.Select(i => i.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            data.PortfolioItems.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(PortfolioItem entity)
    {
        var updated = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            var index = data.PortfolioItems.FindIndex(i => i.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Portfolio item {updated.Id} was not found.");
            }

            ValidateReferences(data, updated);
            FillSubject(data, updated);
            updated.CreatedAt = updated.CreatedAt == default ? data.PortfolioItems[index].CreatedAt : updated.CreatedAt;
            data.PortfolioItems[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data => data.PortfolioItems.RemoveAll(i => i.Id == id));
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.PortfolioItems.Any(i => i.Id == id);
    }

    public async Task<IEnumerable<PortfolioItem>> GetFilteredAsync(PortfolioFilter filter)
    {
        var data = await _store.ReadAsync();
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
        var data = await _store.ReadAsync();
        return data.PortfolioItems
            .Where(i => i.AssignmentId == assignmentId)
            .OrderByDescending(i => i.Date)
            .Select(i => RepositoryProjection.HydratePortfolioItem(data, i))
            .ToList();
    }

    public async Task<IEnumerable<PortfolioItem>> GetByLessonPlanIdAsync(int lessonPlanId)
    {
        var data = await _store.ReadAsync();
        return data.PortfolioItems
            .Where(i => i.LessonPlanId == lessonPlanId)
            .OrderByDescending(i => i.Date)
            .Select(i => RepositoryProjection.HydratePortfolioItem(data, i))
            .ToList();
    }

    private static PortfolioItem Normalize(PortfolioItem item)
    {
        item.Date = item.Date.Date;
        item.Title = item.Title?.Trim() ?? string.Empty;
        item.Description = item.Description?.Trim() ?? string.Empty;
        item.Notes = item.Notes?.Trim() ?? string.Empty;
        item.Subject = item.Subject?.Trim() ?? string.Empty;
        item.ExternalUrl = item.ExternalUrl?.Trim() ?? string.Empty;
        item.Tags = item.Tags?.Trim() ?? string.Empty;
        return item;
    }

    private static void ValidateReferences(HomeschoolData data, PortfolioItem item)
    {
        if (item.StudentId <= 0 || !data.Students.Any(s => s.Id == item.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        if (item.SubjectId <= 0 || !data.Courses.Any(c => c.Id == item.SubjectId))
        {
            throw new InvalidOperationException("A valid subject is required.");
        }

        if (item.AssignmentId.HasValue && !data.Assignments.Any(a => a.Id == item.AssignmentId.Value))
        {
            throw new InvalidOperationException("The linked assignment was not found.");
        }

        if (item.LessonPlanId.HasValue && !data.LessonPlans.Any(lp => lp.Id == item.LessonPlanId.Value))
        {
            throw new InvalidOperationException("The linked lesson plan was not found.");
        }
    }

    private static void FillSubject(HomeschoolData data, PortfolioItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Subject))
        {
            return;
        }

        var course = data.Courses.FirstOrDefault(c => c.Id == item.SubjectId);
        item.Subject = course == null ? string.Empty : string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject;
    }

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
