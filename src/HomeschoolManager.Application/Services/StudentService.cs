using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<Student?> GetStudentByIdAsync(int id)
    {
        return await _studentRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Student>> GetAllStudentsAsync()
    {
        return await _studentRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Student>> GetStudentsByGradeLevelAsync(string gradeLevel)
    {
        return await _studentRepository.GetByGradeLevelAsync(gradeLevel);
    }

    public async Task<Student> CreateStudentAsync(Student student)
    {
        student.CreatedAt = DateTime.UtcNow;
        return await _studentRepository.AddAsync(student);
    }

    public async Task<Student> UpdateStudentAsync(Student student)
    {
        student.UpdatedAt = DateTime.UtcNow;
        await _studentRepository.UpdateAsync(student);
        return student;
    }

    public async Task DeleteStudentAsync(int id)
    {
        await _studentRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<Assignment>> GetStudentAssignmentsAsync(int studentId)
    {
        var student = await _studentRepository.GetWithAssignmentsAsync(studentId);
        return student?.Assignments ?? new List<Assignment>();
    }

    public async Task<IEnumerable<Grade>> GetStudentGradesAsync(int studentId)
    {
        var student = await _studentRepository.GetWithGradesAsync(studentId);
        return student?.Grades ?? new List<Grade>();
    }
}
