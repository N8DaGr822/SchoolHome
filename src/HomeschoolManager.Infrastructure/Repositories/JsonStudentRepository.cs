using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonStudentRepository : IStudentRepository
{
    private readonly HomeschoolDataStore _store;

    public JsonStudentRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<Student?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var student = data.Students.FirstOrDefault(s => s.Id == id);
        return student == null ? null : RepositoryProjection.HydrateStudent(data, student);
    }

    public async Task<IEnumerable<Student>> GetAllAsync()
    {
        var data = await _store.ReadAsync();
        return data.Students
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => RepositoryProjection.HydrateStudent(data, s))
            .ToList();
    }

    public async Task<Student> AddAsync(Student entity)
    {
        var saved = HomeschoolDataStore.Clone(entity);
        await _store.WriteAsync(data =>
        {
            saved.Id = saved.Id == 0 ? NextId(data.Students.Select(s => s.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            data.Students.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(Student entity)
    {
        var updated = HomeschoolDataStore.Clone(entity);
        await _store.WriteAsync(data =>
        {
            var index = data.Students.FindIndex(s => s.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Student {updated.Id} was not found.");
            }

            updated.CreatedAt = updated.CreatedAt == default ? data.Students[index].CreatedAt : updated.CreatedAt;
            data.Students[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data =>
        {
            data.Students.RemoveAll(s => s.Id == id);
            data.Assignments.RemoveAll(a => a.StudentId == id);
            data.Grades.RemoveAll(g => g.StudentId == id);
        });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.Students.Any(s => s.Id == id);
    }

    public async Task<IEnumerable<Student>> GetByGradeLevelAsync(string gradeLevel)
    {
        var data = await _store.ReadAsync();
        return data.Students
            .Where(s => s.GradeLevel.Equals(gradeLevel, StringComparison.OrdinalIgnoreCase))
            .Select(s => RepositoryProjection.HydrateStudent(data, s))
            .ToList();
    }

    public async Task<IEnumerable<Student>> GetActiveStudentsAsync()
    {
        return await GetAllAsync();
    }

    public async Task<Student?> GetWithCoursesAsync(int id)
    {
        return await GetByIdAsync(id);
    }

    public async Task<Student?> GetWithAssignmentsAsync(int id)
    {
        return await GetByIdAsync(id);
    }

    public async Task<Student?> GetWithGradesAsync(int id)
    {
        return await GetByIdAsync(id);
    }

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
