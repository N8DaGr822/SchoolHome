using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<IEnumerable<Assignment>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<Assignment>> GetOpenAssignmentsAsync();
}
