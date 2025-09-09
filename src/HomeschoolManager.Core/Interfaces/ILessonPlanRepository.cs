using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface ILessonPlanRepository : IRepository<LessonPlan>
{
    Task<IEnumerable<LessonPlan>> GetByCourseIdAsync(int courseId);
    Task<IEnumerable<LessonPlan>> GetByWeekNumberAsync(int courseId, int weekNumber);
    Task<IEnumerable<LessonPlan>> GetBySubjectAsync(string subject);
}
