using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface ILearningTimeRepository : IRepository<LearningTimeEntry>
{
    Task<IEnumerable<LearningTimeEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<LearningTimeEntry>> GetByStudentIdAsync(int studentId);
    Task<LearningTimeEntry?> GetBySourceAsync(LearningTimeSource source, int sourceId);
}
