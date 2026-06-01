using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface ICurriculumResourceRepository : IRepository<CurriculumResource>
{
    Task<IEnumerable<CurriculumResource>> GetFilteredAsync(CurriculumResourceFilter filter);
}

public sealed record CurriculumResourceFilter(
    int? SubjectId = null,
    CurriculumResourceType? ResourceType = null);
