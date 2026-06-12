using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonAssignmentRepository : JsonRepositoryBase<Assignment>, IAssignmentRepository
{
    public JsonAssignmentRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<Assignment> Items(HomeschoolData data) => data.Assignments;

    protected override string EntityLabel => "Assignment";

    private protected override Assignment Hydrate(HomeschoolData data, Assignment entity) =>
        RepositoryProjection.HydrateAssignment(data, entity);

    private protected override IEnumerable<Assignment> Order(HomeschoolData data, IEnumerable<Assignment> items) =>
        items.OrderBy(a => a.DueDate);

    private protected override void OnSaving(HomeschoolData data, Assignment entity)
    {
        entity.Subject = string.IsNullOrWhiteSpace(entity.Subject)
            ? data.Courses.FirstOrDefault(c => c.Id == entity.CourseId)?.Subject ?? string.Empty
            : entity.Subject;
        entity.EstimatedMinutes = entity.EstimatedMinutes <= 0 ? null : entity.EstimatedMinutes;
        entity.AssignedDate = entity.AssignedDate == default ? DateTime.UtcNow : entity.AssignedDate;
    }

    private protected override void OnDeleting(HomeschoolData data, int id)
    {
        data.Grades.RemoveAll(g => g.AssignmentId == id);
    }

    public async Task<IEnumerable<Assignment>> GetByStudentIdAsync(int studentId)
    {
        var data = await Store.ReadAsync();
        return data.Assignments
            .Where(a => a.StudentId == studentId)
            .OrderBy(a => a.DueDate)
            .Select(a => RepositoryProjection.HydrateAssignment(data, a))
            .ToList();
    }

    public async Task<IEnumerable<Assignment>> GetOpenAssignmentsAsync()
    {
        var data = await Store.ReadAsync();
        return data.Assignments
            .Where(a => a.Status is AssignmentStatus.Assigned or AssignmentStatus.InProgress or AssignmentStatus.Overdue)
            .OrderBy(a => a.DueDate)
            .Select(a => RepositoryProjection.HydrateAssignment(data, a))
            .ToList();
    }
}
