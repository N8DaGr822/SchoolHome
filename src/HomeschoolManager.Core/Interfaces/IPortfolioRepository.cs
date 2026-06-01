using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface IPortfolioRepository : IRepository<PortfolioItem>
{
    Task<IEnumerable<PortfolioItem>> GetFilteredAsync(PortfolioFilter filter);
    Task<IEnumerable<PortfolioItem>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<PortfolioItem>> GetByAssignmentIdAsync(int assignmentId);
    Task<IEnumerable<PortfolioItem>> GetByLessonPlanIdAsync(int lessonPlanId);
}

public sealed record PortfolioFilter(
    int? StudentId = null,
    int? SubjectId = null,
    PortfolioItemType? Type = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    bool BestWorkOnly = false);
