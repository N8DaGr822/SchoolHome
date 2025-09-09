using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class LessonPlanService : ILessonPlanService
{
    private readonly ILessonPlanRepository _lessonPlanRepository;

    public LessonPlanService(ILessonPlanRepository lessonPlanRepository)
    {
        _lessonPlanRepository = lessonPlanRepository;
    }

    public async Task<LessonPlan?> GetLessonPlanByIdAsync(int id)
    {
        return await _lessonPlanRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<LessonPlan>> GetAllLessonPlansAsync()
    {
        return await _lessonPlanRepository.GetAllAsync();
    }

    public async Task<IEnumerable<LessonPlan>> GetLessonPlansByCourseIdAsync(int courseId)
    {
        return await _lessonPlanRepository.GetByCourseIdAsync(courseId);
    }

    public async Task<IEnumerable<LessonPlan>> GetLessonPlansByWeekAsync(int courseId, int weekNumber)
    {
        return await _lessonPlanRepository.GetByWeekNumberAsync(courseId, weekNumber);
    }

    public async Task<IEnumerable<LessonPlan>> GetLessonPlansBySubjectAsync(string subject)
    {
        return await _lessonPlanRepository.GetBySubjectAsync(subject);
    }

    public async Task<LessonPlan> CreateLessonPlanAsync(LessonPlan lessonPlan)
    {
        lessonPlan.CreatedAt = DateTime.UtcNow;
        return await _lessonPlanRepository.AddAsync(lessonPlan);
    }

    public async Task<LessonPlan> UpdateLessonPlanAsync(LessonPlan lessonPlan)
    {
        lessonPlan.UpdatedAt = DateTime.UtcNow;
        await _lessonPlanRepository.UpdateAsync(lessonPlan);
        return lessonPlan;
    }

    public async Task DeleteLessonPlanAsync(int id)
    {
        await _lessonPlanRepository.DeleteAsync(id);
    }
}
