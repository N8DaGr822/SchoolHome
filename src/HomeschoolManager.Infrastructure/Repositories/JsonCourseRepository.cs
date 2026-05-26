using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonCourseRepository : IRepository<Course>
{
    private readonly HomeschoolDataStore _store;

    public JsonCourseRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<Course?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var course = data.Courses.FirstOrDefault(c => c.Id == id);
        return course == null ? null : RepositoryProjection.HydrateCourse(data, course);
    }

    public async Task<IEnumerable<Course>> GetAllAsync()
    {
        var data = await _store.ReadAsync();
        return data.Courses
            .OrderBy(c => c.Name)
            .Select(c => RepositoryProjection.HydrateCourse(data, c))
            .ToList();
    }

    public async Task<Course> AddAsync(Course entity)
    {
        var saved = HomeschoolDataStore.Clone(entity);
        await _store.WriteAsync(data =>
        {
            saved.Id = saved.Id == 0 ? NextId(data.Courses.Select(c => c.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            foreach (var lessonPlan in saved.LessonPlans)
            {
                lessonPlan.CourseId = saved.Id;
            }

            data.Courses.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(Course entity)
    {
        var updated = HomeschoolDataStore.Clone(entity);
        await _store.WriteAsync(data =>
        {
            var index = data.Courses.FindIndex(c => c.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Course {updated.Id} was not found.");
            }

            updated.CreatedAt = updated.CreatedAt == default ? data.Courses[index].CreatedAt : updated.CreatedAt;
            foreach (var lessonPlan in updated.LessonPlans)
            {
                lessonPlan.CourseId = updated.Id;
            }

            data.Courses[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data =>
        {
            var assignmentIds = data.Assignments
                .Where(a => a.CourseId == id)
                .Select(a => a.Id)
                .ToHashSet();

            data.Courses.RemoveAll(c => c.Id == id);
            data.Assignments.RemoveAll(a => a.CourseId == id);
            data.Grades.RemoveAll(g => assignmentIds.Contains(g.AssignmentId));
        });
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.Courses.Any(c => c.Id == id);
    }

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
