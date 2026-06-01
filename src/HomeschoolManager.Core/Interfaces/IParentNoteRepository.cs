using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface IParentNoteRepository : IRepository<ParentNote>
{
    Task<IEnumerable<ParentNote>> GetFilteredAsync(ParentNoteFilter filter);
    Task<IEnumerable<ParentNote>> GetByStudentIdAsync(int studentId);
}

public sealed record ParentNoteFilter(
    int? StudentId = null,
    int? SubjectId = null,
    int? AssignmentId = null,
    int? LessonPlanId = null,
    ParentNoteCategory? Category = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null);
