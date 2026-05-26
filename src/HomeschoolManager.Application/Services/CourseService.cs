using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class CourseService : ICourseService
{
    private readonly IRepository<Course> _courseRepository;

    public CourseService(IRepository<Course> courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<Course?> GetCourseByIdAsync(int id)
    {
        return await _courseRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Course>> GetAllCoursesAsync()
    {
        return await _courseRepository.GetAllAsync();
    }

    public async Task<Course> CreateCourseAsync(Course course)
    {
        course.CreatedAt = DateTime.UtcNow;
        return await _courseRepository.AddAsync(course);
    }

    public async Task<Course> UpdateCourseAsync(Course course)
    {
        course.UpdatedAt = DateTime.UtcNow;
        await _courseRepository.UpdateAsync(course);
        return course;
    }

    public async Task DeleteCourseAsync(int id)
    {
        await _courseRepository.DeleteAsync(id);
    }

    public async Task<LessonPlan> AddLessonPlanAsync(int courseId, LessonPlan lessonPlan)
    {
        var course = await GetRequiredCourseAsync(courseId);
        lessonPlan.Id = course.LessonPlans.Count > 0 ? course.LessonPlans.Max(lp => lp.Id) + 1 : 1;
        lessonPlan.CourseId = courseId;
        lessonPlan.CreatedAt = DateTime.UtcNow;
        course.LessonPlans.Add(lessonPlan);
        await UpdateCourseAsync(course);
        return lessonPlan;
    }

    public async Task<LessonPlan> UpdateLessonPlanAsync(int courseId, LessonPlan lessonPlan)
    {
        var course = await GetRequiredCourseAsync(courseId);
        var index = course.LessonPlans.FindIndex(lp => lp.Id == lessonPlan.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Lesson plan {lessonPlan.Id} was not found.");
        }

        lessonPlan.CourseId = courseId;
        lessonPlan.UpdatedAt = DateTime.UtcNow;
        course.LessonPlans[index] = lessonPlan;
        await UpdateCourseAsync(course);
        return lessonPlan;
    }

    public async Task DeleteLessonPlanAsync(int courseId, int lessonPlanId)
    {
        var course = await GetRequiredCourseAsync(courseId);
        course.LessonPlans.RemoveAll(lp => lp.Id == lessonPlanId);
        await UpdateCourseAsync(course);
    }

    private async Task<Course> GetRequiredCourseAsync(int courseId)
    {
        return await _courseRepository.GetByIdAsync(courseId)
            ?? throw new InvalidOperationException($"Course {courseId} was not found.");
    }
}
