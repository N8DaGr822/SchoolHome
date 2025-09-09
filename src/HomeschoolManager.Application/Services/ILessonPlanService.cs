using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public interface ILessonPlanService
{
    Task<LessonPlan?> GetLessonPlanByIdAsync(int id);
    Task<IEnumerable<LessonPlan>> GetAllLessonPlansAsync();
    Task<IEnumerable<LessonPlan>> GetLessonPlansByCourseIdAsync(int courseId);
    Task<IEnumerable<LessonPlan>> GetLessonPlansByWeekAsync(int courseId, int weekNumber);
    Task<IEnumerable<LessonPlan>> GetLessonPlansBySubjectAsync(string subject);
    Task<LessonPlan> CreateLessonPlanAsync(LessonPlan lessonPlan);
    Task<LessonPlan> UpdateLessonPlanAsync(LessonPlan lessonPlan);
    Task DeleteLessonPlanAsync(int id);
}
