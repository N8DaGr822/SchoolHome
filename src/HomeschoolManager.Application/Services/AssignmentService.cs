using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ILearningTimeService? _learningTimeService;

    public AssignmentService(
        IAssignmentRepository assignmentRepository,
        ILearningTimeService? learningTimeService = null)
    {
        _assignmentRepository = assignmentRepository;
        _learningTimeService = learningTimeService;
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

    public async Task<Assignment> CompleteAssignmentAsync(int id, bool createLearningTimeEntry = false)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Assignment {id} was not found.");

        assignment.Status = AssignmentStatus.Completed;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _assignmentRepository.UpdateAsync(assignment);

        if (createLearningTimeEntry && _learningTimeService is not null)
        {
            await _learningTimeService.CreateFromAssignmentCompletionAsync(assignment);
        }

        return assignment;
    }

    public async Task DeleteAssignmentAsync(int id)
    {
        await _assignmentRepository.DeleteAsync(id);
    }
}
