using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public interface ICourseService
{
    Task<Course?> GetCourseByIdAsync(int id);
    Task<IEnumerable<Course>> GetAllCoursesAsync();
    Task<Course> CreateCourseAsync(Course course);
    Task<Course> UpdateCourseAsync(Course course);
    Task DeleteCourseAsync(int id);
    Task<LessonPlan> AddLessonPlanAsync(int courseId, LessonPlan lessonPlan);
    Task<LessonPlan> UpdateLessonPlanAsync(int courseId, LessonPlan lessonPlan);
    Task DeleteLessonPlanAsync(int courseId, int lessonPlanId);
}
