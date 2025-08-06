using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface IStudentRepository : IRepository<Student>
{
    Task<IEnumerable<Student>> GetByGradeLevelAsync(string gradeLevel);
    Task<IEnumerable<Student>> GetActiveStudentsAsync();
    Task<Student?> GetWithCoursesAsync(int id);
    Task<Student?> GetWithAssignmentsAsync(int id);
} 