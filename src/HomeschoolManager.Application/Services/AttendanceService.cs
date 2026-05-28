using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;

    public AttendanceService(IAttendanceRepository attendanceRepository)
    {
        _attendanceRepository = attendanceRepository;
    }

    public async Task<AttendanceRecord?> GetAttendanceByIdAsync(int id)
    {
        return await _attendanceRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<AttendanceRecord>> GetAllAttendanceAsync()
    {
        return await _attendanceRepository.GetAllAsync();
    }

    public async Task<IEnumerable<AttendanceRecord>> GetAttendanceByDateAsync(DateTime date)
    {
        return await _attendanceRepository.GetByDateAsync(date.Date);
    }

    public async Task<IEnumerable<AttendanceRecord>> GetAttendanceByMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return await _attendanceRepository.GetByDateRangeAsync(start, end);
    }

    public async Task<IEnumerable<AttendanceRecord>> GetAttendanceForStudentAsync(int studentId)
    {
        return await _attendanceRepository.GetByStudentIdAsync(studentId);
    }

    public async Task<AttendanceRecord?> GetAttendanceForStudentDateAsync(int studentId, DateTime date)
    {
        return await _attendanceRepository.GetByStudentAndDateAsync(studentId, date.Date);
    }

    public async Task<AttendanceRecord> CreateAttendanceAsync(AttendanceRecord attendanceRecord)
    {
        NormalizeForSave(attendanceRecord);
        attendanceRecord.CreatedAt = DateTime.UtcNow;
        return await _attendanceRepository.AddAsync(attendanceRecord);
    }

    public async Task<AttendanceRecord> UpdateAttendanceAsync(AttendanceRecord attendanceRecord)
    {
        NormalizeForSave(attendanceRecord);
        attendanceRecord.UpdatedAt = DateTime.UtcNow;
        await _attendanceRepository.UpdateAsync(attendanceRecord);
        return attendanceRecord;
    }

    public async Task<AttendanceRecord> SaveAttendanceAsync(AttendanceRecord attendanceRecord)
    {
        NormalizeForSave(attendanceRecord);
        var existing = attendanceRecord.Id > 0
            ? await _attendanceRepository.GetByIdAsync(attendanceRecord.Id)
            : await _attendanceRepository.GetByStudentAndDateAsync(attendanceRecord.StudentId, attendanceRecord.Date);

        if (existing == null)
        {
            return await CreateAttendanceAsync(attendanceRecord);
        }

        existing.StudentId = attendanceRecord.StudentId;
        existing.Date = attendanceRecord.Date;
        existing.Status = attendanceRecord.Status;
        existing.Minutes = attendanceRecord.Minutes;
        existing.Notes = attendanceRecord.Notes;
        return await UpdateAttendanceAsync(existing);
    }

    public async Task DeleteAttendanceAsync(int id)
    {
        await _attendanceRepository.DeleteAsync(id);
    }

    private static void NormalizeForSave(AttendanceRecord attendanceRecord)
    {
        attendanceRecord.Date = attendanceRecord.Date.Date;
        attendanceRecord.Notes = attendanceRecord.Notes?.Trim() ?? string.Empty;
    }
}
