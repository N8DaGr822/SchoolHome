using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public interface IStudentService
{
    Task<Student?> GetStudentByIdAsync(int id);
    Task<IEnumerable<Student>> GetAllStudentsAsync();
    Task<IEnumerable<Student>> GetStudentsByGradeLevelAsync(string gradeLevel);
    Task<Student> CreateStudentAsync(Student student);
    Task<Student> UpdateStudentAsync(Student student);
    Task DeleteStudentAsync(int id);
    Task<IEnumerable<Assignment>> GetStudentAssignmentsAsync(int studentId);
    Task<IEnumerable<Grade>> GetStudentGradesAsync(int studentId);
}