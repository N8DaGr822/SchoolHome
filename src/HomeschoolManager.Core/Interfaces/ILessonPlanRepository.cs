using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface ILessonPlanRepository : IRepository<LessonPlan>
{
    Task<IEnumerable<LessonPlan>> GetByWeekAsync(DateTime weekStart, int? studentId = null, int? subjectId = null);
    Task<IEnumerable<LessonPlan>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<LessonPlan>> GetBySubjectIdAsync(int subjectId);
}
