using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;

    public AssignmentService(IAssignmentRepository assignmentRepository)
    {
        _assignmentRepository = assignmentRepository;
    }

    public async Task<Assignment?> GetAssignmentByIdAsync(int id)
    {
        return await _assignmentRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Assignment>> GetAllAssignmentsAsync()
    {
        return await _assignmentRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Assignment>> GetOpenAssignmentsAsync()
    {
        return await _assignmentRepository.GetOpenAssignmentsAsync();
    }

    public async Task<IEnumerable<Assignment>> GetAssignmentsForStudentAsync(int studentId)
    {
        return await _assignmentRepository.GetByStudentIdAsync(studentId);
    }

    public async Task<Assignment> CreateAssignmentAsync(Assignment assignment)
    {
        assignment.AssignedDate = assignment.AssignedDate == default ? DateTime.UtcNow : assignment.AssignedDate;
        assignment.CreatedAt = DateTime.UtcNow;
        return await _assignmentRepository.AddAsync(assignment);
    }

    public async Task<Assignment> UpdateAssignmentAsync(Assignment assignment)
    {
        assignment.UpdatedAt = DateTime.UtcNow;
        await _assignmentRepository.UpdateAsync(assignment);
        return assignment;
    }

    public async Task DeleteAssignmentAsync(int id)
    {
        await _assignmentRepository.DeleteAsync(id);
    }
}
