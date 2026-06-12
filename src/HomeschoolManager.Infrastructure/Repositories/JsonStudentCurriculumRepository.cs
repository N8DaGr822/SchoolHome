using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonStudentCurriculumRepository : JsonRepositoryBase<StudentCurriculum>, IStudentCurriculumRepository
{
    public JsonStudentCurriculumRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<StudentCurriculum> Items(HomeschoolData data) => data.StudentCurricula;

    protected override string EntityLabel => "Student curriculum";

    private protected override StudentCurriculum Hydrate(HomeschoolData data, StudentCurriculum entity) =>
        RepositoryProjection.HydrateStudentCurriculum(data, entity);

    protected override StudentCurriculum Normalize(StudentCurriculum entity)
    {
        entity.CurrentUnit = entity.CurrentUnit?.Trim() ?? string.Empty;
        entity.CurrentLesson = entity.CurrentLesson?.Trim() ?? string.Empty;
        entity.StartDate = entity.StartDate?.Date;
        entity.TargetEndDate = entity.TargetEndDate?.Date;
        return entity;
    }

    private protected override void Validate(HomeschoolData data, StudentCurriculum entity)
    {
        if (entity.StudentId <= 0 || !data.Students.Any(s => s.Id == entity.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        if (entity.CurriculumResourceId <= 0 || !data.CurriculumResources.Any(r => r.Id == entity.CurriculumResourceId))
        {
            throw new InvalidOperationException("A valid curriculum resource is required.");
        }

        if (entity.PercentComplete is < 0 or > 100)
        {
            throw new InvalidOperationException("Percent complete must be between 0 and 100.");
        }

        var duplicate = data.StudentCurricula.Any(c =>
            c.Id != entity.Id &&
            c.StudentId == entity.StudentId &&
            c.CurriculumResourceId == entity.CurriculumResourceId);

        if (duplicate)
        {
            throw new InvalidOperationException("This curriculum resource is already assigned to the selected student.");
        }
    }

    public override async Task<IEnumerable<StudentCurriculum>> GetAllAsync()
    {
        return await GetFilteredAsync(new StudentCurriculumFilter());
    }

    public async Task<IEnumerable<StudentCurriculum>> GetFilteredAsync(StudentCurriculumFilter filter)
    {
        var data = await Store.ReadAsync();
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
        var data = await Store.ReadAsync();
        return data.StudentCurricula
            .Where(c => c.CurriculumResourceId == resourceId)
            .Select(c => RepositoryProjection.HydrateStudentCurriculum(data, c))
            .ToList();
    }

    public async Task<StudentCurriculum?> GetByStudentAndResourceAsync(int studentId, int resourceId)
    {
        var data = await Store.ReadAsync();
        var studentCurriculum = data.StudentCurricula.FirstOrDefault(c =>
            c.StudentId == studentId &&
            c.CurriculumResourceId == resourceId);

        return studentCurriculum == null ? null : RepositoryProjection.HydrateStudentCurriculum(data, studentCurriculum);
    }
}
