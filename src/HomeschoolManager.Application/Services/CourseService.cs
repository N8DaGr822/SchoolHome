using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ILessonPlanRepository _lessonPlanRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IStudentRepository _studentRepository;

    public CourseService(
        ICourseRepository courseRepository,
        ILessonPlanRepository lessonPlanRepository,
        IAssignmentRepository assignmentRepository,
        IStudentRepository studentRepository)
    {
        _courseRepository = courseRepository;
        _lessonPlanRepository = lessonPlanRepository;
        _assignmentRepository = assignmentRepository;
        _studentRepository = studentRepository;
    }

    public async Task<Course?> GetCourseByIdAsync(int id)
    {
        return await _courseRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Course>> GetAllCoursesAsync()
    {
        return await _courseRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Course>> GetCoursesBySubjectAsync(string subject)
    {
        return await _courseRepository.GetBySubjectAsync(subject);
    }

    public async Task<IEnumerable<Course>> GetCoursesByGradeLevelAsync(string gradeLevel)
    {
        return await _courseRepository.GetByGradeLevelAsync(gradeLevel);
    }

    public async Task<IEnumerable<Course>> GetActiveCoursesAsync()
    {
        return await _courseRepository.GetActiveCoursesAsync();
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

    public async Task<IEnumerable<LessonPlan>> GetCourseLessonPlansAsync(int courseId)
    {
        return await _lessonPlanRepository.GetByCourseIdAsync(courseId);
    }

    public async Task<IEnumerable<Assignment>> GetCourseAssignmentsAsync(int courseId)
    {
        return await _assignmentRepository.GetByCourseIdAsync(courseId);
    }

    public async Task<IEnumerable<Student>> GetCourseStudentsAsync(int courseId)
    {
        var course = await _courseRepository.GetWithStudentsAsync(courseId);
        return course?.Students ?? new List<Student>();
    }
}
