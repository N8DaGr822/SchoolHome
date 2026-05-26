using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonAssignmentRepository : IAssignmentRepository
{
    private readonly HomeschoolDataStore _store;

    public JsonAssignmentRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<Assignment?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var assignment = data.Assignments.FirstOrDefault(a => a.Id == id);
        return assignment == null ? null : RepositoryProjection.HydrateAssignment(data, assignment);
    }

    public async Task<IEnumerable<Assignment>> GetAllAsync()
    {
        var data = await _store.ReadAsync();
        return data.Assignments
            .OrderBy(a => a.DueDate)
            .Select(a => RepositoryProjection.HydrateAssignment(data, a))
            .ToList();
    }

    public async Task<Assignment> AddAsync(Assignment entity)
    {
        var saved = HomeschoolDataStore.Clone(entity);
        await _store.WriteAsync(data =>
        {
            saved.Id = saved.Id == 0 ? NextId(data.Assignments.Select(a => a.Id)) : saved.Id;
            saved.Subject = string.IsNullOrWhiteSpace(saved.Subject)
                ? data.Courses.FirstOrDefault(c => c.Id == saved.CourseId)?.Subject ?? string.Empty
                : saved.Subject;
            saved.AssignedDate = saved.AssignedDate == default ? DateTime.UtcNow : saved.AssignedDate;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            data.Assignments.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(Assignment entity)
    {
        var updated = HomeschoolDataStore.Clone(entity);
        await _store.WriteAsync(data =>
        {
            var index = data.Assignments.FindIndex(a => a.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Assignment {updated.Id} was not found.");
            }

            updated.Subject = string.IsNullOrWhiteSpace(updated.Subject)
                ? data.Courses.FirstOrDefault(c => c.Id == updated.CourseId)?.Subject ?? string.Empty
                : updated.Subject;
            updated.CreatedAt = updated.CreatedAt == default ? data.Assignments[index].CreatedAt : updated.CreatedAt;
            data.Assignments[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data =>
        {
            data.Assignments.RemoveAll(a => a.Id == id);
            data.Grades.RemoveAll(g => g.AssignmentId == id);
        });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.Assignments.Any(a => a.Id == id);
    }

    public async Task<IEnumerable<Assignment>> GetByStudentIdAsync(int studentId)
    {
        var data = await _store.ReadAsync();
        return data.Assignments
            .Where(a => a.StudentId == studentId)
            .OrderBy(a => a.DueDate)
            .Select(a => RepositoryProjection.HydrateAssignment(data, a))
            .ToList();
    }

    public async Task<IEnumerable<Assignment>> GetOpenAssignmentsAsync()
    {
        var data = await _store.ReadAsync();
        return data.Assignments
            .Where(a => a.Status is AssignmentStatus.Assigned or AssignmentStatus.InProgress or AssignmentStatus.Overdue)
            .OrderBy(a => a.DueDate)
            .Select(a => RepositoryProjection.HydrateAssignment(data, a))
            .ToList();
    }

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
