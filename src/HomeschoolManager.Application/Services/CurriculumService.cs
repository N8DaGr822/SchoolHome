using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class CurriculumService : ICurriculumService
{
    private readonly ICurriculumResourceRepository _resourceRepository;
    private readonly IStudentCurriculumRepository _studentCurriculumRepository;

    public CurriculumService(
        ICurriculumResourceRepository resourceRepository,
        IStudentCurriculumRepository studentCurriculumRepository)
    {
        _resourceRepository = resourceRepository;
        _studentCurriculumRepository = studentCurriculumRepository;
    }

    public async Task<CurriculumResource?> GetResourceByIdAsync(int id)
    {
        return await _resourceRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<CurriculumResource>> GetResourcesAsync(CurriculumResourceFilter filter)
    {
        return await _resourceRepository.GetFilteredAsync(filter);
    }

    public async Task<CurriculumResource> CreateResourceAsync(CurriculumResource resource)
    {
        NormalizeResource(resource);
        resource.CreatedAt = DateTime.UtcNow;
        return await _resourceRepository.AddAsync(resource);
    }

    public async Task<CurriculumResource> UpdateResourceAsync(CurriculumResource resource)
    {
        NormalizeResource(resource);
        var existing = await _resourceRepository.GetByIdAsync(resource.Id)
            ?? throw new InvalidOperationException($"Curriculum resource {resource.Id} was not found.");

        resource.CreatedAt = existing.CreatedAt;
        resource.UpdatedAt = DateTime.UtcNow;
        await _resourceRepository.UpdateAsync(resource);
        return resource;
    }

    public async Task DeleteResourceAsync(int id)
    {
        await _resourceRepository.DeleteAsync(id);
    }

    public async Task<StudentCurriculum?> GetStudentCurriculumByIdAsync(int id)
    {
        return await _studentCurriculumRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<StudentCurriculum>> GetStudentCurriculaAsync(StudentCurriculumFilter filter)
    {
        return await _studentCurriculumRepository.GetFilteredAsync(filter);
    }

    public async Task<IEnumerable<StudentCurriculum>> AssignResourceAsync(
        int resourceId,
        IEnumerable<int> studentIds,
        DateTime? startDate,
        DateTime? targetEndDate)
    {
        if (!await _resourceRepository.ExistsAsync(resourceId))
        {
            throw new InvalidOperationException("Curriculum resource was not found.");
        }

        var assigned = new List<StudentCurriculum>();
        foreach (var studentId in studentIds.Where(id => id > 0).Distinct())
        {
            var existing = await _studentCurriculumRepository.GetByStudentAndResourceAsync(studentId, resourceId);
            if (existing is not null)
            {
                assigned.Add(existing);
                continue;
            }

            assigned.Add(await _studentCurriculumRepository.AddAsync(new StudentCurriculum
            {
                StudentId = studentId,
                CurriculumResourceId = resourceId,
                Status = CurriculumStatus.NotStarted,
                StartDate = startDate?.Date,
                TargetEndDate = targetEndDate?.Date,
                CreatedAt = DateTime.UtcNow
            }));
        }

        if (assigned.Count == 0)
        {
            throw new InvalidOperationException("Select at least one student.");
        }

        return assigned;
    }

    public async Task<StudentCurriculum> UpdateStudentCurriculumAsync(StudentCurriculum studentCurriculum)
    {
        NormalizeStudentCurriculum(studentCurriculum);
        var existing = await _studentCurriculumRepository.GetByIdAsync(studentCurriculum.Id)
            ?? throw new InvalidOperationException($"Student curriculum {studentCurriculum.Id} was not found.");

        studentCurriculum.StudentId = existing.StudentId;
        studentCurriculum.CurriculumResourceId = existing.CurriculumResourceId;
        studentCurriculum.CreatedAt = existing.CreatedAt;
        studentCurriculum.UpdatedAt = DateTime.UtcNow;
        await _studentCurriculumRepository.UpdateAsync(studentCurriculum);
        return studentCurriculum;
    }

    public async Task DeleteStudentCurriculumAsync(int id)
    {
        await _studentCurriculumRepository.DeleteAsync(id);
    }

    private static void NormalizeResource(CurriculumResource resource)
    {
        resource.Title = resource.Title?.Trim() ?? string.Empty;
        resource.Description = resource.Description?.Trim() ?? string.Empty;
        resource.Publisher = resource.Publisher?.Trim() ?? string.Empty;
        resource.Author = resource.Author?.Trim() ?? string.Empty;
        resource.Url = resource.Url?.Trim() ?? string.Empty;
        resource.GradeLevel = resource.GradeLevel?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(resource.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }
    }

    private static void NormalizeStudentCurriculum(StudentCurriculum studentCurriculum)
    {
        studentCurriculum.CurrentUnit = studentCurriculum.CurrentUnit?.Trim() ?? string.Empty;
        studentCurriculum.CurrentLesson = studentCurriculum.CurrentLesson?.Trim() ?? string.Empty;
        studentCurriculum.StartDate = studentCurriculum.StartDate?.Date;
        studentCurriculum.TargetEndDate = studentCurriculum.TargetEndDate?.Date;

        if (studentCurriculum.PercentComplete is < 0 or > 100)
        {
            throw new InvalidOperationException("Percent complete must be between 0 and 100.");
        }
    }
}
