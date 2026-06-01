using HomeschoolManager.Application.Services;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Xunit;

namespace HomeschoolManager.Tests.Unit;

public class YearbookServiceTests
{
    [Fact]
    public async Task CreateYearbookAsync_GeneratesDefaultStudentPagesWithPortfolioAndFieldTrips()
    {
        var repository = new FakeYearbookRepository();
        var service = CreateService(repository);

        var yearbook = await service.CreateYearbookAsync(new Yearbook
        {
            FamilyId = 1,
            Title = "Ava Yearbook",
            SchoolYear = "2026-2027",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 6, 30),
            Scope = YearbookScope.Student,
            StudentId = 1
        });

        Assert.Equal(9, yearbook.Pages.Count);
        Assert.Contains(yearbook.Pages, p => p.Title == "Student Profile");
        Assert.Contains(yearbook.Pages, p => p.Title == "Portfolio Showcase" && p.ContentJson.Contains("Watercolor"));
        Assert.Contains(yearbook.Pages, p => p.Title == "Field Trips" && p.ContentJson.Contains("Museum"));
    }

    [Fact]
    public async Task CreateYearbookAsync_ValidatesStudentScope()
    {
        var service = CreateService(new FakeYearbookRepository());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateYearbookAsync(new Yearbook
            {
                FamilyId = 1,
                Title = "Student Yearbook",
                SchoolYear = "2026-2027",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2027, 6, 30),
                Scope = YearbookScope.Student
            }));

        Assert.Contains("Student is required", exception.Message);
    }

    [Fact]
    public async Task MovePageAsync_ReordersPages()
    {
        var repository = new FakeYearbookRepository();
        var service = CreateService(repository);
        var yearbook = await service.CreateYearbookAsync(new Yearbook
        {
            FamilyId = 1,
            Title = "Family Yearbook",
            SchoolYear = "2026-2027",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 6, 30),
            Scope = YearbookScope.Family
        });
        var secondPage = yearbook.Pages.OrderBy(p => p.SortOrder).Skip(1).First();

        await service.MovePageAsync(yearbook.Id, secondPage.Id, -1);
        var reloaded = await service.GetYearbookByIdAsync(yearbook.Id);

        Assert.Equal(secondPage.Id, reloaded!.Pages.OrderBy(p => p.SortOrder).First().Id);
    }

    [Fact]
    public async Task SavePagesAsync_RejectsNegativeSortOrderAndInvalidJson()
    {
        var repository = new FakeYearbookRepository();
        var service = CreateService(repository);
        var yearbook = await service.CreateYearbookAsync(new Yearbook
        {
            FamilyId = 1,
            Title = "Family Yearbook",
            SchoolYear = "2026-2027",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 6, 30),
            Scope = YearbookScope.Family
        });
        var page = yearbook.Pages.First();
        page.SortOrder = -1;
        page.ContentJson = "{broken";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdatePageAsync(page));

        Assert.Contains("non-negative", exception.Message);
    }

    [Fact]
    public async Task SavePortfolioSelectionsAsync_StoresSelectedPortfolioItemsAsAssets()
    {
        var repository = new FakeYearbookRepository();
        var service = CreateService(repository);
        var yearbook = await service.CreateYearbookAsync(new Yearbook
        {
            FamilyId = 1,
            Title = "Ava Yearbook",
            SchoolYear = "2026-2027",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 6, 30),
            Scope = YearbookScope.Student,
            StudentId = 1
        });

        var portfolioPage = yearbook.Pages.First(p => p.Title == "Portfolio Showcase");

        await service.SavePortfolioSelectionsAsync(yearbook.Id, new Dictionary<int, IEnumerable<int>>
        {
            [portfolioPage.Id] = new[] { 1 }
        });
        var reloaded = await service.GetYearbookByIdAsync(yearbook.Id);

        Assert.Single(reloaded!.Assets);
        Assert.Equal(portfolioPage.Id, reloaded.Assets[0].YearbookPageId);
        Assert.Equal(1, reloaded.Assets[0].PortfolioItemId);
        Assert.Equal("Watercolor", reloaded.Assets[0].Title);
    }

    private static YearbookService CreateService(FakeYearbookRepository repository)
    {
        return new YearbookService(
            repository,
            new FakeStudentRepository([
                new Student { Id = 1, FirstName = "Ava", LastName = "Stone", GradeLevel = "6th", DateOfBirth = new DateTime(2014, 1, 1) }
            ]),
            new FakePortfolioRepository([
                new PortfolioItem { Id = 1, StudentId = 1, Title = "Watercolor", Description = "A landscape painting.", Date = new DateTime(2026, 9, 1) }
            ]),
            new FakeAttendanceRepository([
                new AttendanceRecord { Id = 1, StudentId = 1, Date = new DateTime(2026, 10, 1), Status = AttendanceStatus.FieldTrip, Notes = "Museum visit" }
            ]));
    }

    private sealed class FakeYearbookRepository : IYearbookRepository
    {
        private readonly List<Yearbook> _yearbooks = new();
        private readonly List<YearbookPage> _pages = new();
        private readonly List<YearbookAsset> _assets = new();

        public Task<Yearbook?> GetByIdAsync(int id)
        {
            var yearbook = _yearbooks.FirstOrDefault(y => y.Id == id);
            return Task.FromResult(yearbook == null ? null : Clone(yearbook));
        }

        public Task<IEnumerable<Yearbook>> GetAllAsync() => Task.FromResult<IEnumerable<Yearbook>>(_yearbooks.Select(Clone));
        public Task<IEnumerable<Yearbook>> GetByFamilyIdAsync(int familyId) => Task.FromResult<IEnumerable<Yearbook>>(_yearbooks.Where(y => y.FamilyId == familyId).Select(Clone));

        public Task<Yearbook> AddAsync(Yearbook entity)
        {
            entity.Id = entity.Id == 0 ? _yearbooks.Count + 1 : entity.Id;
            _yearbooks.Add(Clone(entity));
            return Task.FromResult(Clone(entity));
        }

        public Task UpdateAsync(Yearbook entity)
        {
            _yearbooks.RemoveAll(y => y.Id == entity.Id);
            _yearbooks.Add(Clone(entity));
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _yearbooks.RemoveAll(y => y.Id == id);
            _pages.RemoveAll(p => p.YearbookId == id);
            _assets.RemoveAll(a => a.YearbookId == id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id) => Task.FromResult(_yearbooks.Any(y => y.Id == id));
        public Task<IEnumerable<YearbookPage>> GetPagesAsync(int yearbookId) => Task.FromResult<IEnumerable<YearbookPage>>(_pages.Where(p => p.YearbookId == yearbookId).OrderBy(p => p.SortOrder).Select(Clone));
        public Task<YearbookPage?> GetPageByIdAsync(int pageId) => Task.FromResult(_pages.FirstOrDefault(p => p.Id == pageId) is { } page ? Clone(page) : null);

        public Task<YearbookPage> AddPageAsync(YearbookPage page)
        {
            page.Id = page.Id == 0 ? _pages.Count + 1 : page.Id;
            _pages.Add(Clone(page));
            return Task.FromResult(Clone(page));
        }

        public Task UpdatePageAsync(YearbookPage page)
        {
            _pages.RemoveAll(p => p.Id == page.Id);
            _pages.Add(Clone(page));
            return Task.CompletedTask;
        }

        public Task<IEnumerable<YearbookAsset>> GetAssetsAsync(int yearbookId)
        {
            return Task.FromResult<IEnumerable<YearbookAsset>>(_assets.Where(a => a.YearbookId == yearbookId).Select(Clone));
        }

        public Task SaveAssetsAsync(int yearbookId, IEnumerable<YearbookAsset> assets)
        {
            _assets.RemoveAll(a => a.YearbookId == yearbookId);
            foreach (var asset in assets)
            {
                asset.Id = asset.Id == 0 ? _assets.Count + 1 : asset.Id;
                asset.YearbookId = yearbookId;
                _assets.Add(Clone(asset));
            }

            return Task.CompletedTask;
        }

        public Task DeletePageAsync(int pageId)
        {
            _pages.RemoveAll(p => p.Id == pageId);
            return Task.CompletedTask;
        }

        public Task SavePagesAsync(int yearbookId, IEnumerable<YearbookPage> pages)
        {
            _pages.RemoveAll(p => p.YearbookId == yearbookId);
            foreach (var page in pages)
            {
                page.Id = page.Id == 0 ? _pages.Count + 1 : page.Id;
                page.YearbookId = yearbookId;
                _pages.Add(Clone(page));
            }

            return Task.CompletedTask;
        }

        private Yearbook Clone(Yearbook yearbook)
        {
            return new Yearbook
            {
                Id = yearbook.Id,
                FamilyId = yearbook.FamilyId,
                Title = yearbook.Title,
                SchoolYear = yearbook.SchoolYear,
                StartDate = yearbook.StartDate,
                EndDate = yearbook.EndDate,
                Scope = yearbook.Scope,
                StudentId = yearbook.StudentId,
                Pages = _pages.Where(p => p.YearbookId == yearbook.Id).OrderBy(p => p.SortOrder).Select(Clone).ToList(),
                Assets = _assets.Where(a => a.YearbookId == yearbook.Id).Select(Clone).ToList()
            };
        }

        private static YearbookPage Clone(YearbookPage page)
        {
            return new YearbookPage
            {
                Id = page.Id,
                YearbookId = page.YearbookId,
                Title = page.Title,
                SortOrder = page.SortOrder,
                IsHidden = page.IsHidden,
                ContentJson = page.ContentJson,
                Elements = page.Elements.Select(Clone).ToList(),
                CreatedAt = page.CreatedAt,
                UpdatedAt = page.UpdatedAt
            };
        }

        private static PageElement Clone(PageElement element)
        {
            return new PageElement
            {
                Id = element.Id,
                Type = element.Type,
                X = element.X,
                Y = element.Y,
                Width = element.Width,
                Height = element.Height,
                Rotation = element.Rotation,
                ZIndex = element.ZIndex,
                PhotoId = element.PhotoId,
                Src = element.Src,
                ObjectFit = element.ObjectFit,
                Text = element.Text,
                FontSize = element.FontSize,
                FontFamily = element.FontFamily,
                FontWeight = element.FontWeight,
                Color = element.Color,
                TextAlign = element.TextAlign
            };
        }

        private static YearbookAsset Clone(YearbookAsset asset)
        {
            return new YearbookAsset
            {
                Id = asset.Id,
                YearbookId = asset.YearbookId,
                YearbookPageId = asset.YearbookPageId,
                PortfolioItemId = asset.PortfolioItemId,
                Title = asset.Title,
                SourcePath = asset.SourcePath,
                Caption = asset.Caption,
                CreatedAt = asset.CreatedAt
            };
        }
    }

    private sealed class FakeStudentRepository : IStudentRepository
    {
        private readonly List<Student> _students;
        public FakeStudentRepository(IEnumerable<Student> students) => _students = students.ToList();
        public Task<Student?> GetByIdAsync(int id) => Task.FromResult(_students.FirstOrDefault(s => s.Id == id));
        public Task<IEnumerable<Student>> GetAllAsync() => Task.FromResult<IEnumerable<Student>>(_students);
        public Task<Student> AddAsync(Student entity) => Task.FromResult(entity);
        public Task UpdateAsync(Student entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_students.Any(s => s.Id == id));
        public Task<IEnumerable<Student>> GetByGradeLevelAsync(string gradeLevel) => Task.FromResult<IEnumerable<Student>>(_students.Where(s => s.GradeLevel == gradeLevel));
        public Task<IEnumerable<Student>> GetActiveStudentsAsync() => Task.FromResult<IEnumerable<Student>>(_students);
        public Task<Student?> GetWithCoursesAsync(int id) => GetByIdAsync(id);
        public Task<Student?> GetWithAssignmentsAsync(int id) => GetByIdAsync(id);
        public Task<Student?> GetWithGradesAsync(int id) => GetByIdAsync(id);
    }

    private sealed class FakePortfolioRepository : IPortfolioRepository
    {
        private readonly List<PortfolioItem> _items;
        public FakePortfolioRepository(IEnumerable<PortfolioItem> items) => _items = items.ToList();
        public Task<PortfolioItem?> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault(i => i.Id == id));
        public Task<IEnumerable<PortfolioItem>> GetAllAsync() => Task.FromResult<IEnumerable<PortfolioItem>>(_items);
        public Task<PortfolioItem> AddAsync(PortfolioItem entity) => Task.FromResult(entity);
        public Task UpdateAsync(PortfolioItem entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_items.Any(i => i.Id == id));
        public Task<IEnumerable<PortfolioItem>> GetByStudentIdAsync(int studentId) => Task.FromResult<IEnumerable<PortfolioItem>>(_items.Where(i => i.StudentId == studentId));
        public Task<IEnumerable<PortfolioItem>> GetByAssignmentIdAsync(int assignmentId) => Task.FromResult<IEnumerable<PortfolioItem>>(_items.Where(i => i.AssignmentId == assignmentId));
        public Task<IEnumerable<PortfolioItem>> GetByLessonPlanIdAsync(int lessonPlanId) => Task.FromResult<IEnumerable<PortfolioItem>>(_items.Where(i => i.LessonPlanId == lessonPlanId));
        public Task<IEnumerable<PortfolioItem>> GetFilteredAsync(PortfolioFilter filter)
        {
            var query = _items.AsEnumerable();
            if (filter.StudentId.HasValue)
            {
                query = query.Where(i => i.StudentId == filter.StudentId.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(i => i.Date.Date >= filter.StartDate.Value.Date);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(i => i.Date.Date <= filter.EndDate.Value.Date);
            }

            return Task.FromResult<IEnumerable<PortfolioItem>>(query);
        }
    }

    private sealed class FakeAttendanceRepository : IAttendanceRepository
    {
        private readonly List<AttendanceRecord> _records;
        public FakeAttendanceRepository(IEnumerable<AttendanceRecord> records) => _records = records.ToList();
        public Task<AttendanceRecord?> GetByIdAsync(int id) => Task.FromResult(_records.FirstOrDefault(a => a.Id == id));
        public Task<IEnumerable<AttendanceRecord>> GetAllAsync() => Task.FromResult<IEnumerable<AttendanceRecord>>(_records);
        public Task<AttendanceRecord> AddAsync(AttendanceRecord entity) => Task.FromResult(entity);
        public Task UpdateAsync(AttendanceRecord entity) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<bool> ExistsAsync(int id) => Task.FromResult(_records.Any(a => a.Id == id));
        public Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateTime date) => Task.FromResult<IEnumerable<AttendanceRecord>>(_records.Where(a => a.Date.Date == date.Date));
        public Task<IEnumerable<AttendanceRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) => Task.FromResult<IEnumerable<AttendanceRecord>>(_records.Where(a => a.Date.Date >= startDate.Date && a.Date.Date <= endDate.Date));
        public Task<IEnumerable<AttendanceRecord>> GetByStudentIdAsync(int studentId) => Task.FromResult<IEnumerable<AttendanceRecord>>(_records.Where(a => a.StudentId == studentId));
        public Task<AttendanceRecord?> GetByStudentAndDateAsync(int studentId, DateTime date) => Task.FromResult(_records.FirstOrDefault(a => a.StudentId == studentId && a.Date.Date == date.Date));
    }
}
