using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public interface ICurriculumService
{
    Task<CurriculumResource?> GetResourceByIdAsync(int id);
    Task<IEnumerable<CurriculumResource>> GetResourcesAsync(CurriculumResourceFilter filter);
    Task<CurriculumResource> CreateResourceAsync(CurriculumResource resource);
    Task<CurriculumResource> UpdateResourceAsync(CurriculumResource resource);
    Task DeleteResourceAsync(int id);
    Task<StudentCurriculum?> GetStudentCurriculumByIdAsync(int id);
    Task<IEnumerable<StudentCurriculum>> GetStudentCurriculaAsync(StudentCurriculumFilter filter);
    Task<IEnumerable<StudentCurriculum>> AssignResourceAsync(int resourceId, IEnumerable<int> studentIds, DateTime? startDate, DateTime? targetEndDate);
    Task<StudentCurriculum> UpdateStudentCurriculumAsync(StudentCurriculum studentCurriculum);
    Task DeleteStudentCurriculumAsync(int id);
}
