using System.Text.Json;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeschoolManager.Application.Services;

public class YearbookService : IYearbookService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IYearbookRepository _yearbookRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ILogger<YearbookService> _logger;

    public YearbookService(
        IYearbookRepository yearbookRepository,
        IStudentRepository studentRepository,
        IPortfolioRepository portfolioRepository,
        IAttendanceRepository attendanceRepository,
        ILogger<YearbookService>? logger = null)
    {
        _yearbookRepository = yearbookRepository;
        _studentRepository = studentRepository;
        _portfolioRepository = portfolioRepository;
        _attendanceRepository = attendanceRepository;
        _logger = logger ?? NullLogger<YearbookService>.Instance;
    }

    public async Task<IEnumerable<Yearbook>> GetYearbooksAsync(int familyId = 1)
    {
        return await _yearbookRepository.GetByFamilyIdAsync(familyId);
    }

    public async Task<Yearbook?> GetYearbookByIdAsync(int id)
    {
        return await _yearbookRepository.GetByIdAsync(id);
    }

    public async Task<Yearbook> CreateYearbookAsync(Yearbook yearbook)
    {
        ValidateYearbook(yearbook);
        yearbook.CreatedAt = DateTime.UtcNow;
        var saved = await _yearbookRepository.AddAsync(yearbook);
        var pages = await GenerateDefaultPagesAsync(saved);
        await _yearbookRepository.SavePagesAsync(saved.Id, pages);
        return await _yearbookRepository.GetByIdAsync(saved.Id) ?? saved;
    }

    public async Task<Yearbook> UpdateYearbookAsync(Yearbook yearbook)
    {
        ValidateYearbook(yearbook);
        var existing = await _yearbookRepository.GetByIdAsync(yearbook.Id)
            ?? throw new InvalidOperationException($"Yearbook {yearbook.Id} was not found.");

        yearbook.CreatedAt = existing.CreatedAt;
        yearbook.UpdatedAt = DateTime.UtcNow;
        await _yearbookRepository.UpdateAsync(yearbook);
        return yearbook;
    }

    public async Task DeleteYearbookAsync(int id)
    {
        await _yearbookRepository.DeleteAsync(id);
    }

    public async Task<YearbookPage> AddCustomPageAsync(int yearbookId)
    {
        var pages = (await _yearbookRepository.GetPagesAsync(yearbookId)).ToList();
        return await _yearbookRepository.AddPageAsync(new YearbookPage
        {
            YearbookId = yearbookId,
            Title = "Custom Page",
            SortOrder = pages.Count == 0 ? 0 : pages.Max(p => p.SortOrder) + 1,
            ContentJson = CreateContentJson("Custom Page", "Add memories, photos, and reflections here."),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task UpdatePageAsync(YearbookPage page)
    {
        ValidatePage(page);
        page.UpdatedAt = DateTime.UtcNow;
        await _yearbookRepository.UpdatePageAsync(page);
    }

    public async Task DeletePageAsync(int pageId)
    {
        await _yearbookRepository.DeletePageAsync(pageId);
    }

    public async Task MovePageAsync(int yearbookId, int pageId, int direction)
    {
        var pages = (await _yearbookRepository.GetPagesAsync(yearbookId)).OrderBy(p => p.SortOrder).ToList();
        var index = pages.FindIndex(p => p.Id == pageId);
        var targetIndex = index + direction;
        if (index < 0 || targetIndex < 0 || targetIndex >= pages.Count)
        {
            return;
        }

        (pages[index], pages[targetIndex]) = (pages[targetIndex], pages[index]);
        for (var i = 0; i < pages.Count; i++)
        {
            pages[i].SortOrder = i;
            pages[i].UpdatedAt = DateTime.UtcNow;
        }

        await _yearbookRepository.SavePagesAsync(yearbookId, pages);
    }

    public async Task SavePagesAsync(int yearbookId, IEnumerable<YearbookPage> pages)
    {
        var normalized = pages.OrderBy(p => p.SortOrder).ToList();
        for (var i = 0; i < normalized.Count; i++)
        {
            normalized[i].SortOrder = i;
            ValidatePage(normalized[i]);
            normalized[i].UpdatedAt = DateTime.UtcNow;
        }

        await _yearbookRepository.SavePagesAsync(yearbookId, normalized);
    }

    public async Task<IEnumerable<PortfolioItem>> GetPortfolioCandidatesAsync(int yearbookId)
    {
        var yearbook = await _yearbookRepository.GetByIdAsync(yearbookId)
            ?? throw new InvalidOperationException($"Yearbook {yearbookId} was not found.");

        return await GetPortfolioItemsAsync(yearbook);
    }

    public async Task SavePortfolioSelectionsAsync(int yearbookId, IReadOnlyDictionary<int, IEnumerable<int>> pagePortfolioItemIds)
    {
        var yearbook = await _yearbookRepository.GetByIdAsync(yearbookId)
            ?? throw new InvalidOperationException($"Yearbook {yearbookId} was not found.");
        var candidates = (await GetPortfolioItemsAsync(yearbook)).ToDictionary(i => i.Id);

        var assets = new List<YearbookAsset>();
        foreach (var selection in pagePortfolioItemIds)
        {
            var page = yearbook.Pages.FirstOrDefault(p => p.Id == selection.Key);
            if (page is null)
            {
                continue;
            }

            foreach (var itemId in selection.Value.Where(id => id > 0).Distinct())
            {
                if (!candidates.TryGetValue(itemId, out var item))
                {
                    continue;
                }

                assets.Add(new YearbookAsset
                {
                    YearbookId = yearbookId,
                    YearbookPageId = page.Id,
                    PortfolioItemId = item.Id,
                    Title = item.Title,
                    SourcePath = !string.IsNullOrWhiteSpace(item.StoredFilePath) ? item.StoredFilePath : item.ExternalUrl,
                    Caption = item.Description,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _yearbookRepository.SaveAssetsAsync(yearbookId, assets);
    }

    private async Task<IReadOnlyList<YearbookPage>> GenerateDefaultPagesAsync(Yearbook yearbook)
    {
        var portfolioItems = await GetPortfolioItemsAsync(yearbook);
        var fieldTrips = await GetFieldTripsAsync(yearbook);
        var students = yearbook.Scope == YearbookScope.Student && yearbook.StudentId.HasValue
            ? (await _studentRepository.GetByIdAsync(yearbook.StudentId.Value)) is { } student ? new[] { student } : Array.Empty<Student>()
            : (await _studentRepository.GetAllAsync()).ToArray();

        var pageTemplates = new List<(string Title, string Body)>
        {
            ("Cover", $"{yearbook.Title}\n{yearbook.SchoolYear}"),
            ("Year At A Glance", $"A look back at learning from {yearbook.StartDate:MMMM d, yyyy} through {yearbook.EndDate:MMMM d, yyyy}."),
            (yearbook.Scope == YearbookScope.Student ? "Student Profile" : "Family Intro", CreateIntroBody(yearbook, students)),
            ("Portfolio Showcase", CreatePortfolioBody(portfolioItems)),
            ("Field Trips", CreateFieldTripBody(fieldTrips)),
            ("Achievements", "Add achievements, milestones, awards, and favorite accomplishments."),
            ("Parent Letter", "Write a letter celebrating growth, persistence, and memorable moments."),
            ("Student Reflection", "Add student reflections, favorite lessons, and goals for next year."),
            ("Closing Page", "Close the yearbook with a favorite quote, memory, or family note.")
        };

        return pageTemplates
            .Select((template, index) => new YearbookPage
            {
                YearbookId = yearbook.Id,
                Title = template.Title,
                SortOrder = index,
                ContentJson = CreateContentJson(template.Title, template.Body),
                CreatedAt = DateTime.UtcNow
            })
            .ToList();
    }

    private async Task<IReadOnlyList<PortfolioItem>> GetPortfolioItemsAsync(Yearbook yearbook)
    {
        try
        {
            return (await _portfolioRepository.GetFilteredAsync(new PortfolioFilter(
                    StudentId: yearbook.Scope == YearbookScope.Student ? yearbook.StudentId : null,
                    StartDate: yearbook.StartDate,
                    EndDate: yearbook.EndDate,
                    BestWorkOnly: false)))
                .OrderBy(i => i.Date)
                .ThenBy(i => i.Title)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException)
        {
            _logger.LogWarning(ex, "Could not load portfolio items for yearbook {YearbookId}; continuing with an empty set.", yearbook.Id);
            return Array.Empty<PortfolioItem>();
        }
    }

    private async Task<IReadOnlyList<AttendanceRecord>> GetFieldTripsAsync(Yearbook yearbook)
    {
        try
        {
            return (await _attendanceRepository.GetByDateRangeAsync(yearbook.StartDate, yearbook.EndDate))
                .Where(a => a.Status == AttendanceStatus.FieldTrip)
                .Where(a => yearbook.Scope == YearbookScope.Family || a.StudentId == yearbook.StudentId)
                .OrderBy(a => a.Date)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException)
        {
            _logger.LogWarning(ex, "Could not load field trips for yearbook {YearbookId}; continuing with an empty set.", yearbook.Id);
            return Array.Empty<AttendanceRecord>();
        }
    }

    private static string CreateIntroBody(Yearbook yearbook, IReadOnlyList<Student> students)
    {
        if (yearbook.Scope == YearbookScope.Student)
        {
            var student = students.FirstOrDefault();
            return student is null
                ? "Add a student profile, interests, favorite subjects, and highlights."
                : $"{student.FirstName} {student.LastName}\nGrade {student.GradeLevel}\nAdd interests, favorite subjects, and highlights.";
        }

        return students.Count == 0
            ? "Introduce your homeschool year and family rhythm."
            : $"This yearbook celebrates {students.Count} student(s): {string.Join(", ", students.Select(s => $"{s.FirstName} {s.LastName}"))}.";
    }

    private static string CreatePortfolioBody(IReadOnlyList<PortfolioItem> items)
    {
        if (items.Count == 0)
        {
            return "No portfolio items were found for this date range yet. Add best work, photos, files, or notes to fill this section.";
        }

        return string.Join("\n", items.Select(i => $"{i.Date:MM/dd/yyyy} - {i.Title}: {i.Description}"));
    }

    private static string CreateFieldTripBody(IReadOnlyList<AttendanceRecord> fieldTrips)
    {
        if (fieldTrips.Count == 0)
        {
            return "No field trip records were found for this date range yet. Add field trips to attendance notes to fill this section.";
        }

        return string.Join("\n", fieldTrips.Select(f => $"{f.Date:MM/dd/yyyy} - {f.Notes}"));
    }

    public static string GetPlainText(YearbookPage page)
    {
        try
        {
            using var document = JsonDocument.Parse(page.ContentJson);
            return document.RootElement.TryGetProperty("body", out var body)
                ? body.GetString() ?? string.Empty
                : page.ContentJson;
        }
        catch (JsonException)
        {
            return page.ContentJson;
        }
    }

    public static void SetPlainText(YearbookPage page, string body)
    {
        page.ContentJson = CreateContentJson(page.Title, body ?? string.Empty);
    }

    private static string CreateContentJson(string heading, string body)
    {
        return JsonSerializer.Serialize(new YearbookPageContent(heading, body), JsonOptions);
    }

    private static void ValidateYearbook(Yearbook yearbook)
    {
        yearbook.Title = yearbook.Title?.Trim() ?? string.Empty;
        yearbook.SchoolYear = yearbook.SchoolYear?.Trim() ?? string.Empty;
        yearbook.StartDate = yearbook.StartDate.Date;
        yearbook.EndDate = yearbook.EndDate.Date;

        if (string.IsNullOrWhiteSpace(yearbook.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(yearbook.SchoolYear))
        {
            throw new InvalidOperationException("School year is required.");
        }

        if (yearbook.EndDate < yearbook.StartDate)
        {
            throw new InvalidOperationException("Start date must be before or equal to end date.");
        }

        if (yearbook.Scope == YearbookScope.Student && !yearbook.StudentId.HasValue)
        {
            throw new InvalidOperationException("Student is required when scope is Student.");
        }
    }

    private static void ValidatePage(YearbookPage page)
    {
        page.Title = page.Title?.Trim() ?? string.Empty;
        page.ContentJson = string.IsNullOrWhiteSpace(page.ContentJson) ? "{}" : page.ContentJson.Trim();
        YearbookPageMigration.EnsureElements(page);

        if (string.IsNullOrWhiteSpace(page.Title))
        {
            throw new InvalidOperationException("Page title is required.");
        }

        if (page.SortOrder < 0)
        {
            throw new InvalidOperationException("Page sort order must be non-negative.");
        }

        try
        {
            using var _ = JsonDocument.Parse(page.ContentJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("ContentJson must be valid JSON.", ex);
        }
    }

    private sealed record YearbookPageContent(string Heading, string Body);
}
