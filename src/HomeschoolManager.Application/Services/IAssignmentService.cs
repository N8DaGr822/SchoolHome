using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public interface IAssignmentService
{
    Task<Assignment?> GetAssignmentByIdAsync(int id);
    Task<IEnumerable<Assignment>> GetAllAssignmentsAsync();
    Task<IEnumerable<Assignment>> GetAssignmentsByStudentIdAsync(int studentId);
    Task<IEnumerable<Assignment>> GetAssignmentsByCourseIdAsync(int courseId);
    Task<IEnumerable<Assignment>> GetAssignmentsByStatusAsync(AssignmentStatus status);
    Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync();
    Task<IEnumerable<Assignment>> GetDueSoonAssignmentsAsync(int daysAhead = 7);
    Task<Assignment> CreateAssignmentAsync(Assignment assignment);
    Task<Assignment> UpdateAssignmentAsync(Assignment assignment);
    Task DeleteAssignmentAsync(int id);
    Task<Assignment> GradeAssignmentAsync(int assignmentId, string grade);
}
