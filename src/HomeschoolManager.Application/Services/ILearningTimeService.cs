using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public interface ILearningTimeService
{
    Task<LearningTimeEntry?> GetEntryByIdAsync(int id);
    Task<IEnumerable<LearningTimeEntry>> GetEntriesAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<LearningTimeEntry>> GetEntriesForStudentAsync(int studentId);
    Task<LearningTimeEntry> CreateEntryAsync(LearningTimeEntry entry);
    Task<LearningTimeEntry> UpdateEntryAsync(LearningTimeEntry entry);
    Task DeleteEntryAsync(int id);
    Task<LearningTimeReport> GetReportAsync(DateTime startDate, DateTime endDate);
    Task<LearningTimeEntry?> CreateFromAssignmentCompletionAsync(Assignment assignment);
    Task<LearningTimeEntry?> CreateFromLessonCompletionAsync(LessonPlan lessonPlan);
}
