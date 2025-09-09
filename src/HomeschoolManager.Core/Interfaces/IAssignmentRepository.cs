using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<IEnumerable<Assignment>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<Assignment>> GetByCourseIdAsync(int courseId);
    Task<IEnumerable<Assignment>> GetByStatusAsync(AssignmentStatus status);
    Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync();
    Task<IEnumerable<Assignment>> GetDueSoonAsync(int daysAhead = 7);
    Task<Assignment?> GetWithGradesAsync(int id);
}
