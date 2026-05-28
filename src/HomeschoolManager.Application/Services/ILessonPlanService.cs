using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public interface ILessonPlanService
{
    Task<LessonPlan?> GetLessonPlanByIdAsync(int id);
    Task<IEnumerable<LessonPlan>> GetAllLessonPlansAsync();
    Task<IEnumerable<LessonPlan>> GetWeeklyLessonPlansAsync(DateTime weekStart, int? studentId = null, int? subjectId = null);
    Task<LessonPlan> CreateLessonPlanAsync(LessonPlan lessonPlan);
    Task<LessonPlan> UpdateLessonPlanAsync(LessonPlan lessonPlan);
    Task DeleteLessonPlanAsync(int id);
    Task<LessonPlan> CompleteLessonPlanAsync(int id, bool createLearningTimeEntry = false);
    Task<LessonPlan> SkipLessonPlanAsync(int id);
    Task<LessonPlan> MoveLessonPlanAsync(int id, DateTime plannedDate);
    Task<Assignment> ConvertToAssignmentAsync(int id);
}
