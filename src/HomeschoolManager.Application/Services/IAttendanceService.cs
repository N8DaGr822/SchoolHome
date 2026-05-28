using HomeschoolManager.Core.Entities;

namespace HomeschoolManager.Application.Services;

public interface IAttendanceService
{
    Task<AttendanceRecord?> GetAttendanceByIdAsync(int id);
    Task<IEnumerable<AttendanceRecord>> GetAllAttendanceAsync();
    Task<IEnumerable<AttendanceRecord>> GetAttendanceByDateAsync(DateTime date);
    Task<IEnumerable<AttendanceRecord>> GetAttendanceByMonthAsync(int year, int month);
    Task<IEnumerable<AttendanceRecord>> GetAttendanceForStudentAsync(int studentId);
    Task<AttendanceRecord?> GetAttendanceForStudentDateAsync(int studentId, DateTime date);
    Task<AttendanceRecord> CreateAttendanceAsync(AttendanceRecord attendanceRecord);
    Task<AttendanceRecord> UpdateAttendanceAsync(AttendanceRecord attendanceRecord);
    Task<AttendanceRecord> SaveAttendanceAsync(AttendanceRecord attendanceRecord);
    Task DeleteAttendanceAsync(int id);
}
