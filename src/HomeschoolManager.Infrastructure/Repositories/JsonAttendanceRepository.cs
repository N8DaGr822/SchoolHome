using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonAttendanceRepository : IAttendanceRepository
{
    private readonly HomeschoolDataStore _store;

    public JsonAttendanceRepository(HomeschoolDataStore store)
    {
        _store = store;
    }

    public async Task<AttendanceRecord?> GetByIdAsync(int id)
    {
        var data = await _store.ReadAsync();
        var attendanceRecord = data.AttendanceRecords.FirstOrDefault(a => a.Id == id);
        return attendanceRecord == null ? null : RepositoryProjection.HydrateAttendanceRecord(data, attendanceRecord);
    }

    public async Task<IEnumerable<AttendanceRecord>> GetAllAsync()
    {
        var data = await _store.ReadAsync();
        return data.AttendanceRecords
            .OrderByDescending(a => a.Date)
            .ThenBy(a => GetStudentName(data, a.StudentId))
            .Select(a => RepositoryProjection.HydrateAttendanceRecord(data, a))
            .ToList();
    }

    public async Task<AttendanceRecord> AddAsync(AttendanceRecord entity)
    {
        var saved = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            ValidateStudentExists(data, saved.StudentId);
            EnsureUniqueStudentDate(data, saved.StudentId, saved.Date, ignoreId: null);
            saved.Id = saved.Id == 0 ? NextId(data.AttendanceRecords.Select(a => a.Id)) : saved.Id;
            saved.CreatedAt = saved.CreatedAt == default ? DateTime.UtcNow : saved.CreatedAt;
            data.AttendanceRecords.Add(saved);
        });

        return await GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task UpdateAsync(AttendanceRecord entity)
    {
        var updated = Normalize(HomeschoolDataStore.Clone(entity));
        await _store.WriteAsync(data =>
        {
            var index = data.AttendanceRecords.FindIndex(a => a.Id == updated.Id);
            if (index < 0)
            {
                throw new InvalidOperationException($"Attendance record {updated.Id} was not found.");
            }

            ValidateStudentExists(data, updated.StudentId);
            EnsureUniqueStudentDate(data, updated.StudentId, updated.Date, updated.Id);
            updated.CreatedAt = updated.CreatedAt == default ? data.AttendanceRecords[index].CreatedAt : updated.CreatedAt;
            data.AttendanceRecords[index] = updated;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _store.WriteAsync(data => data.AttendanceRecords.RemoveAll(a => a.Id == id));
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var data = await _store.ReadAsync();
        return data.AttendanceRecords.Any(a => a.Id == id);
    }

    public async Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateTime date)
    {
        var targetDate = date.Date;
        var data = await _store.ReadAsync();
        return data.AttendanceRecords
            .Where(a => a.Date.Date == targetDate)
            .OrderBy(a => GetStudentName(data, a.StudentId))
            .Select(a => RepositoryProjection.HydrateAttendanceRecord(data, a))
            .ToList();
    }

    public async Task<IEnumerable<AttendanceRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        if (end < start)
        {
            (start, end) = (end, start);
        }

        var data = await _store.ReadAsync();
        return data.AttendanceRecords
            .Where(a => a.Date.Date >= start && a.Date.Date <= end)
            .OrderBy(a => a.Date)
            .ThenBy(a => GetStudentName(data, a.StudentId))
            .Select(a => RepositoryProjection.HydrateAttendanceRecord(data, a))
            .ToList();
    }

    public async Task<IEnumerable<AttendanceRecord>> GetByStudentIdAsync(int studentId)
    {
        var data = await _store.ReadAsync();
        return data.AttendanceRecords
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.Date)
            .Select(a => RepositoryProjection.HydrateAttendanceRecord(data, a))
            .ToList();
    }

    public async Task<AttendanceRecord?> GetByStudentAndDateAsync(int studentId, DateTime date)
    {
        var targetDate = date.Date;
        var data = await _store.ReadAsync();
        var attendanceRecord = data.AttendanceRecords
            .FirstOrDefault(a => a.StudentId == studentId && a.Date.Date == targetDate);

        return attendanceRecord == null ? null : RepositoryProjection.HydrateAttendanceRecord(data, attendanceRecord);
    }

    private static AttendanceRecord Normalize(AttendanceRecord attendanceRecord)
    {
        attendanceRecord.Date = attendanceRecord.Date.Date;
        attendanceRecord.Notes = attendanceRecord.Notes?.Trim() ?? string.Empty;
        return attendanceRecord;
    }

    private static void ValidateStudentExists(HomeschoolData data, int studentId)
    {
        if (studentId <= 0 || !data.Students.Any(s => s.Id == studentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }
    }

    private static void EnsureUniqueStudentDate(
        HomeschoolData data,
        int studentId,
        DateTime date,
        int? ignoreId)
    {
        var duplicate = data.AttendanceRecords.Any(a =>
            a.StudentId == studentId &&
            a.Date.Date == date.Date &&
            (!ignoreId.HasValue || a.Id != ignoreId.Value));

        if (duplicate)
        {
            throw new InvalidOperationException($"Attendance for this student is already recorded on {date:MM/dd/yyyy}.");
        }
    }

    private static string GetStudentName(HomeschoolData data, int studentId)
    {
        var student = data.Students.FirstOrDefault(s => s.Id == studentId);
        return student == null ? string.Empty : $"{student.LastName}, {student.FirstName}";
    }

    private static int NextId(IEnumerable<int> ids)
    {
        return ids.DefaultIfEmpty(0).Max() + 1;
    }
}
