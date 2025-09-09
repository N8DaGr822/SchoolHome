using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface ICourseRepository : IRepository<Course>
{
    Task<IEnumerable<Course>> GetBySubjectAsync(string subject);
    Task<IEnumerable<Course>> GetByGradeLevelAsync(string gradeLevel);
    Task<IEnumerable<Course>> GetActiveCoursesAsync();
    Task<Course?> GetWithLessonPlansAsync(int id);
    Task<Course?> GetWithAssignmentsAsync(int id);
    Task<Course?> GetWithStudentsAsync(int id);
}
