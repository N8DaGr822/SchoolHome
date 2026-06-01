using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface IStudentCurriculumRepository : IRepository<StudentCurriculum>
{
    Task<IEnumerable<StudentCurriculum>> GetFilteredAsync(StudentCurriculumFilter filter);
    Task<IEnumerable<StudentCurriculum>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<StudentCurriculum>> GetByResourceIdAsync(int resourceId);
    Task<StudentCurriculum?> GetByStudentAndResourceAsync(int studentId, int resourceId);
}

public sealed record StudentCurriculumFilter(
    int? StudentId = null,
    int? SubjectId = null,
    CurriculumStatus? Status = null,
    CurriculumResourceType? ResourceType = null);
