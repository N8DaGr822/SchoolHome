using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonStudentCurriculumRepository : IStudentCurriculumRepository
{
    private readonly HomeschoolDataStore _store;

    public JsonStudentCurriculumRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<StudentCurriculum?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var studentCurriculum = data.StudentCurricula.FirstOrDefault(c => c.Id == id);
        return studentCurriculum == null ? null : RepositoryProjection.HydrateStudentCurriculum(data, studentCurriculum);
    }

    public async Task<IEnumerable<StudentCurriculum>> GetAllAsync()
    {
        return await GetFilteredAsync(new StudentCurriculumFilter());
    }

    public async Task<StudentCurriculum> AddAsync(StudentCurriculum entity)
    {
        var saved = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            ValidateReferences(data, saved);
            ValidateDuplicate(data, saved);
            saved.Id = saved.Id == 0 ? NextId(data.StudentCurricula.Select(c => c.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            data.StudentCurricula.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(StudentCurriculum entity)
    {
        var updated = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            var index = data.StudentCurricula.FindIndex(c => c.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Student curriculum {updated.Id} was not found.");
            }

            ValidateReferences(data, updated);
            ValidateDuplicate(data, updated, updated.Id);
            updated.CreatedAt = updated.CreatedAt == default ? data.StudentCurricula[index].CreatedAt : updated.CreatedAt;
            data.StudentCurricula[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data => data.StudentCurricula.RemoveAll(c => c.Id == id));
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.StudentCurricula.Any(c => c.Id == id);
    }

    public async Task<IEnumerable<StudentCurriculum>> GetFilteredAsync(StudentCurriculumFilter filter)
    {
        var data = await _store.ReadAsync();
        var query = data.StudentCurricula.AsEnumerable();

        if (filter.StudentId.HasValue && filter.StudentId.Value > 0)
        {
            query = query.Where(c => c.StudentId == filter.StudentId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(c => c.Status == filter.Status.Value);
        }

        if (filter.SubjectId.HasValue && filter.SubjectId.Value > 0)
        {
            query = query.Where(c => data.CurriculumResources.Any(r =>
                r.Id == c.CurriculumResourceId &&
                r.SubjectId == filter.SubjectId.Value));
        }

        if (filter.ResourceType.HasValue)
        {
            query = query.Where(c => data.CurriculumResources.Any(r =>
                r.Id == c.CurriculumResourceId &&
                r.ResourceType == filter.ResourceType.Value));
        }

        return query
            .OrderBy(c => c.Status)
            .ThenBy(c => c.TargetEndDate ?? DateTime.MaxValue)
            .Select(c => RepositoryProjection.HydrateStudentCurriculum(data, c))
            .ToList();
    }

    public async Task<IEnumerable<StudentCurriculum>> GetByStudentIdAsync(int studentId)
    {
        return await GetFilteredAsync(new StudentCurriculumFilter(StudentId: studentId));
    }

    public async Task<IEnumerable<StudentCurriculum>> GetByResourceIdAsync(int resourceId)
    {
        var data = await _store.ReadAsync();
        return data.StudentCurricula
            .Where(c => c.CurriculumResourceId == resourceId)
            .Select(c => RepositoryProjection.HydrateStudentCurriculum(data, c))
            .ToList();
    }

    public async Task<StudentCurriculum?> GetByStudentAndResourceAsync(int studentId, int resourceId)
    {
        var data = await _store.ReadAsync();
        var studentCurriculum = data.StudentCurricula.FirstOrDefault(c =>
            c.StudentId == studentId &&
            c.CurriculumResourceId == resourceId);

        return studentCurriculum == null ? null : RepositoryProjection.HydrateStudentCurriculum(data, studentCurriculum);
    }

    private static StudentCurriculum Normalize(StudentCurriculum studentCurriculum)
    {
        studentCurriculum.CurrentUnit = studentCurriculum.CurrentUnit?.Trim() ?? string.Empty;
        studentCurriculum.CurrentLesson = studentCurriculum.CurrentLesson?.Trim() ?? string.Empty;
        studentCurriculum.StartDate = studentCurriculum.StartDate?.Date;
        studentCurriculum.TargetEndDate = studentCurriculum.TargetEndDate?.Date;
        return studentCurriculum;
    }

    private static void ValidateReferences(HomeschoolData data, StudentCurriculum studentCurriculum)
    {
        if (studentCurriculum.StudentId <= 0 || !data.Students.Any(s => s.Id == studentCurriculum.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        if (studentCurriculum.CurriculumResourceId <= 0 || !data.CurriculumResources.Any(r => r.Id == studentCurriculum.CurriculumResourceId))
        {
            throw new InvalidOperationException("A valid curriculum resource is required.");
        }

        if (studentCurriculum.PercentComplete is < 0 or > 100)
        {
            throw new InvalidOperationException("Percent complete must be between 0 and 100.");
        }
    }

    private static void ValidateDuplicate(
        HomeschoolData data,
        StudentCurriculum studentCurriculum,
        int? ignoreId = null)
    {
        var duplicate = data.StudentCurricula.Any(c =>
            c.Id != ignoreId &&
            c.StudentId == studentCurriculum.StudentId &&
            c.CurriculumResourceId == studentCurriculum.CurriculumResourceId);

        if (duplicate)
        {
            throw new InvalidOperationException("This curriculum resource is already assigned to the selected student.");
        }
    }

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
