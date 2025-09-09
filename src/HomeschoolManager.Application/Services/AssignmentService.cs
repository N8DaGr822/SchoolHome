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

    public async Task<IEnumerable<Assignment>> GetAssignmentsByStudentIdAsync(int studentId)
    {
        return await _assignmentRepository.GetByStudentIdAsync(studentId);
    }

    public async Task<IEnumerable<Assignment>> GetAssignmentsByCourseIdAsync(int courseId)
    {
        return await _assignmentRepository.GetByCourseIdAsync(courseId);
    }

    public async Task<IEnumerable<Assignment>> GetAssignmentsByStatusAsync(AssignmentStatus status)
    {
        return await _assignmentRepository.GetByStatusAsync(status);
    }

    public async Task<IEnumerable<Assignment>> GetOverdueAssignmentsAsync()
    {
        return await _assignmentRepository.GetOverdueAssignmentsAsync();
    }

    public async Task<IEnumerable<Assignment>> GetDueSoonAssignmentsAsync(int daysAhead = 7)
    {
        return await _assignmentRepository.GetDueSoonAsync(daysAhead);
    }

    public async Task<Assignment> CreateAssignmentAsync(Assignment assignment)
    {
        assignment.CreatedAt = DateTime.UtcNow;
        assignment.AssignedDate = DateTime.UtcNow;
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

    public async Task<Assignment> GradeAssignmentAsync(int assignmentId, string grade)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
        if (assignment == null)
        {
            throw new ArgumentException($"Assignment with ID {assignmentId} not found.");
        }

        assignment.Grade = grade;
        assignment.Status = AssignmentStatus.Completed;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _assignmentRepository.UpdateAsync(assignment);
        return assignment;
    }
}
