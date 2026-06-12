using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;

namespace HomeschoolManager.Infrastructure.Repositories;

public class JsonAttendanceRepository : JsonRepositoryBase<AttendanceRecord>, IAttendanceRepository
{
    public JsonAttendanceRepository(HomeschoolDataStore store)
        : base(store)
    {
    }

    private protected override List<AttendanceRecord> Items(HomeschoolData data) => data.AttendanceRecords;

    protected override string EntityLabel => "Attendance record";

    private protected override AttendanceRecord Hydrate(HomeschoolData data, AttendanceRecord entity) =>
        RepositoryProjection.HydrateAttendanceRecord(data, entity);

    protected override AttendanceRecord Normalize(AttendanceRecord entity)
    {
        entity.Date = entity.Date.Date;
        entity.Notes = entity.Notes?.Trim() ?? string.Empty;
        return entity;
    }

    private protected override IEnumerable<AttendanceRecord> Order(HomeschoolData data, IEnumerable<AttendanceRecord> items) =>
        items.OrderByDescending(a => a.Date).ThenBy(a => GetStudentName(data, a.StudentId));

    private protected override void Validate(HomeschoolData data, AttendanceRecord entity)
    {
        if (entity.StudentId <= 0 || !data.Students.Any(s => s.Id == entity.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        var duplicate = data.AttendanceRecords.Any(a =>
            a.StudentId == entity.StudentId &&
            a.Date.Date == entity.Date.Date &&
            a.Id != entity.Id);

        if (duplicate)
        {
            throw new InvalidOperationException($"Attendance for this student is already recorded on {entity.Date:MM/dd/yyyy}.");
        }
    }

    public async Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateTime date)
    {
        var targetDate = date.Date;
        var data = await Store.ReadAsync();
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

        var data = await Store.ReadAsync();
        return data.AttendanceRecords
            .Where(a => a.Date.Date >= start && a.Date.Date <= end)
            .OrderBy(a => a.Date)
            .ThenBy(a => GetStudentName(data, a.StudentId))
            .Select(a => RepositoryProjection.HydrateAttendanceRecord(data, a))
            .ToList();
    }

    public async Task<IEnumerable<AttendanceRecord>> GetByStudentIdAsync(int studentId)
    {
        var data = await Store.ReadAsync();
        return data.AttendanceRecords
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.Date)
            .Select(a => RepositoryProjection.HydrateAttendanceRecord(data, a))
            .ToList();
    }

    public async Task<AttendanceRecord?> GetByStudentAndDateAsync(int studentId, DateTime date)
    {
        var targetDate = date.Date;
        var data = await Store.ReadAsync();
        var attendanceRecord = data.AttendanceRecords
            .FirstOrDefault(a => a.StudentId == studentId && a.Date.Date == targetDate);

        return attendanceRecord == null ? null : RepositoryProjection.HydrateAttendanceRecord(data, attendanceRecord);
    }

    private static string GetStudentName(HomeschoolData data, int studentId)
    {
        var student = data.Students.FirstOrDefault(s => s.Id == studentId);
        return student == null ? string.Empty : $"{student.LastName}, {student.FirstName}";
    }
}
