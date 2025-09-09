using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public interface ICourseService
{
    Task<Course?> GetCourseByIdAsync(int id);
    Task<IEnumerable<Course>> GetAllCoursesAsync();
    Task<IEnumerable<Course>> GetCoursesBySubjectAsync(string subject);
    Task<IEnumerable<Course>> GetCoursesByGradeLevelAsync(string gradeLevel);
    Task<IEnumerable<Course>> GetActiveCoursesAsync();
    Task<Course> CreateCourseAsync(Course course);
    Task<Course> UpdateCourseAsync(Course course);
    Task DeleteCourseAsync(int id);
    Task<IEnumerable<LessonPlan>> GetCourseLessonPlansAsync(int courseId);
    Task<IEnumerable<Assignment>> GetCourseAssignmentsAsync(int courseId);
    Task<IEnumerable<Student>> GetCourseStudentsAsync(int courseId);
}
