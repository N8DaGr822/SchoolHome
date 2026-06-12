using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class AttendanceServiceTests
{
    [Fact]
    public async Task SaveAttendanceAsync_CreatesRecordWhenNoneExistsForStudentAndDate()
    {
        var repository = new FakeAttendanceRepository([]);
        var service = new AttendanceService(repository);

        var saved = await service.SaveAttendanceAsync(new AttendanceRecord
        {
            StudentId = 1,
            Date = new DateTime(2026, 5, 4, 13, 30, 0),
            Status = AttendanceStatus.Present,
            Minutes = 240
        });

        Assert.True(saved.Id > 0);
        Assert.Equal(new DateTime(2026, 5, 4), saved.Date);
        Assert.NotEqual(default, saved.CreatedAt);
    }

    [Fact]
    public async Task SaveAttendanceAsync_UpdatesExistingRecordForSameStudentAndDate()
    {
        var repository = new FakeAttendanceRepository([
            new AttendanceRecord
            {
                Id = 1,
                StudentId = 1,
                Date = new DateTime(2026, 5, 4),
                Status = AttendanceStatus.Absent,
                Minutes = 0,
                CreatedAt = new DateTime(2026, 5, 4)
            }
        ]);
        var service = new AttendanceService(repository);

        var saved = await service.SaveAttendanceAsync(new AttendanceRecord
        {
            StudentId = 1,
            Date = new DateTime(2026, 5, 4),
            Status = AttendanceStatus.Present,
            Minutes = 180,
            Notes = "  arrived late  "
        });

        Assert.Equal(1, saved.Id);
        Assert.Equal(AttendanceStatus.Present, saved.Status);
        Assert.Equal(180, saved.Minutes);
        Assert.NotNull(saved.UpdatedAt);
        Assert.Single(await repository.GetAllAsync());
    }

    [Fact]
    public async Task CreateAttendanceAsync_NormalizesDateAndNotes()
    {
        var repository = new FakeAttendanceRepository([]);
        var service = new AttendanceService(repository);

        var created = await service.CreateAttendanceAsync(new AttendanceRecord
        {
            StudentId = 1,
            Date = new DateTime(2026, 5, 4, 9, 15, 0),
            Status = AttendanceStatus.FieldTrip,
            Notes = "  zoo trip  "
        });

        Assert.Equal(new DateTime(2026, 5, 4), created.Date);
        Assert.Equal("zoo trip", created.Notes);
    }

    [Fact]
    public async Task GetAttendanceByMonthAsync_QueriesFullMonthRange()
    {
        var repository = new FakeAttendanceRepository([
            new AttendanceRecord { Id = 1, StudentId = 1, Date = new DateTime(2026, 4, 30) },
            new AttendanceRecord { Id = 2, StudentId = 1, Date = new DateTime(2026, 5, 1) },
            new AttendanceRecord { Id = 3, StudentId = 1, Date = new DateTime(2026, 5, 31) },
            new AttendanceRecord { Id = 4, StudentId = 1, Date = new DateTime(2026, 6, 1) }
        ]);
        var service = new AttendanceService(repository);

        var may = (await service.GetAttendanceByMonthAsync(2026, 5)).ToList();

        Assert.Equal(2, may.Count);
        Assert.DoesNotContain(may, a => a.Id is 1 or 4);
    }

    private sealed class FakeAttendanceRepository : IAttendanceRepository
    {
        private readonly List<AttendanceRecord> _records;

        public FakeAttendanceRepository(IEnumerable<AttendanceRecord> records)
        {
            _records = records.ToList();
        }

        public Task<AttendanceRecord?> GetByIdAsync(int id) => Task.FromResult(_records.FirstOrDefault(a => a.Id == id));
        public Task<IEnumerable<AttendanceRecord>> GetAllAsync() => Task.FromResult<IEnumerable<AttendanceRecord>>(_records);

        public Task<AttendanceRecord> AddAsync(AttendanceRecord entity)
        {
            entity.Id = _records.Select(a => a.Id).DefaultIfEmpty(0).Max() + 1;
            _records.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(AttendanceRecord entity)
        {
            var index = _records.FindIndex(a => a.Id == entity.Id);
            if (index >= 0)
            {
                _records[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _records.RemoveAll(a => a.Id == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id) => Task.FromResult(_records.Any(a => a.Id == id));

        public Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateTime date) =>
            Task.FromResult<IEnumerable<AttendanceRecord>>(_records.Where(a => a.Date.Date == date.Date));

        public Task<IEnumerable<AttendanceRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) =>
            Task.FromResult<IEnumerable<AttendanceRecord>>(_records.Where(a =>
                a.Date.Date >= startDate.Date && a.Date.Date <= endDate.Date));

        public Task<IEnumerable<AttendanceRecord>> GetByStudentIdAsync(int studentId) =>
            Task.FromResult<IEnumerable<AttendanceRecord>>(_records.Where(a => a.StudentId == studentId));

        public Task<AttendanceRecord?> GetByStudentAndDateAsync(int studentId, DateTime date) =>
            Task.FromResult(_records.FirstOrDefault(a => a.StudentId == studentId && a.Date.Date == date.Date));
    }
}
