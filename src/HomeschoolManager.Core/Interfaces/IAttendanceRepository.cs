using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Core.Interfaces;

public interface IAttendanceRepository : IRepository<AttendanceRecord>
{
    Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateTime date);
    Task<IEnumerable<AttendanceRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<AttendanceRecord>> GetByStudentIdAsync(int studentId);
    Task<AttendanceRecord?> GetByStudentAndDateAsync(int studentId, DateTime date);
}
