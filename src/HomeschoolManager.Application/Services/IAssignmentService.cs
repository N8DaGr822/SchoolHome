using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public interface IAssignmentService
{
    Task<Assignment?> GetAssignmentByIdAsync(int id);
    Task<IEnumerable<Assignment>> GetAllAssignmentsAsync();
    Task<IEnumerable<Assignment>> GetOpenAssignmentsAsync();
    Task<IEnumerable<Assignment>> GetAssignmentsForStudentAsync(int studentId);
    Task<Assignment> CreateAssignmentAsync(Assignment assignment);
    Task<Assignment> UpdateAssignmentAsync(Assignment assignment);
    Task DeleteAssignmentAsync(int id);
}
