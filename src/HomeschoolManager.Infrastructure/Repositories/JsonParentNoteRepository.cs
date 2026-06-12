using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonParentNoteRepository : JsonRepositoryBase<ParentNote>, IParentNoteRepository
{
    public JsonParentNoteRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<ParentNote> Items(HomeschoolData data) => data.ParentNotes;

    protected override string EntityLabel => "Parent note";

    private protected override ParentNote Hydrate(HomeschoolData data, ParentNote entity) =>
        RepositoryProjection.HydrateParentNote(data, entity);

    protected override ParentNote Normalize(ParentNote entity)
    {
        entity.Title = entity.Title?.Trim() ?? string.Empty;
        entity.Content = entity.Content?.Trim() ?? string.Empty;
        entity.NoteDate = entity.NoteDate.Date;
        return entity;
    }

    private protected override void Validate(HomeschoolData data, ParentNote entity)
    {
        if (entity.StudentId <= 0 || !data.Students.Any(s => s.Id == entity.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        if (entity.SubjectId.HasValue && !data.Courses.Any(c => c.Id == entity.SubjectId.Value))
        {
            throw new InvalidOperationException("The linked subject was not found.");
        }

        if (entity.AssignmentId.HasValue && !data.Assignments.Any(a => a.Id == entity.AssignmentId.Value))
        {
            throw new InvalidOperationException("The linked assignment was not found.");
        }

        if (entity.AssignmentId.HasValue && data.Assignments.First(a => a.Id == entity.AssignmentId.Value).StudentId != entity.StudentId)
        {
            throw new InvalidOperationException("The linked assignment belongs to a different student.");
        }

        if (entity.LessonPlanId.HasValue && !data.LessonPlans.Any(lp => lp.Id == entity.LessonPlanId.Value))
        {
            throw new InvalidOperationException("The linked lesson plan was not found.");
        }

        if (entity.LessonPlanId.HasValue && data.LessonPlans.First(lp => lp.Id == entity.LessonPlanId.Value).StudentId != entity.StudentId)
        {
            throw new InvalidOperationException("The linked lesson plan belongs to a different student.");
        }
    }

    public override async Task<IEnumerable<ParentNote>> GetAllAsync()
    {
        return await GetFilteredAsync(new ParentNoteFilter());
    }

    public async Task<IEnumerable<ParentNote>> GetFilteredAsync(ParentNoteFilter filter)
    {
        var data = await Store.ReadAsync();
        var query = data.ParentNotes.AsEnumerable();

        if (filter.StudentId.HasValue && filter.StudentId.Value > 0)
        {
            query = query.Where(n => n.StudentId == filter.StudentId.Value);
        }

        if (filter.SubjectId.HasValue && filter.SubjectId.Value > 0)
        {
            query = query.Where(n => n.SubjectId == filter.SubjectId.Value);
        }

        if (filter.AssignmentId.HasValue && filter.AssignmentId.Value > 0)
        {
            query = query.Where(n => n.AssignmentId == filter.AssignmentId.Value);
        }

        if (filter.LessonPlanId.HasValue && filter.LessonPlanId.Value > 0)
        {
            query = query.Where(n => n.LessonPlanId == filter.LessonPlanId.Value);
        }

        if (filter.Category.HasValue)
        {
            query = query.Where(n => n.Category == filter.Category.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(n => n.NoteDate.Date >= filter.StartDate.Value.Date);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(n => n.NoteDate.Date <= filter.EndDate.Value.Date);
        }

        return query
            .OrderByDescending(n => n.NoteDate)
            .ThenByDescending(n => n.CreatedAt)
            .Select(n => RepositoryProjection.HydrateParentNote(data, n))
            .ToList();
    }

    public async Task<IEnumerable<ParentNote>> GetByStudentIdAsync(int studentId)
    {
        return await GetFilteredAsync(new ParentNoteFilter(StudentId: studentId));
    }
}
