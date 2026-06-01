using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;
using HomeschoolManager.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace HomeschoolManager.Tests.Integration;

public class JsonRepositoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _dataFilePath;

    public JsonRepositoryTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", Guid.NewGuid().ToString("N"));
        _dataFilePath = Path.Combine(_testDirectory, "homeschool-data.json");
    }

    [Fact]
    public async Task StudentRepository_PersistsAddedStudentAcrossStoreInstances()
    {
        var firstRepository = new JsonStudentRepository(new HomeschoolDataStore(_dataFilePath));
        var created = await firstRepository.AddAsync(new Student
        {
            FirstName = "Noah",
            LastName = "Parker",
            DateOfBirth = new DateTime(2013, 2, 8),
            GradeLevel = "6th",
            EnrollmentDate = DateTime.Today
        });

        var secondRepository = new JsonStudentRepository(new HomeschoolDataStore(_dataFilePath));
        var reloaded = await secondRepository.GetByIdAsync(created.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("Noah", reloaded.FirstName);
        Assert.Equal("Parker", reloaded.LastName);
    }

    [Fact]
    public async Task AssignmentRepository_CompletedAssignmentIsRemovedFromOpenAssignments()
    {
        var repository = new JsonAssignmentRepository(new HomeschoolDataStore(_dataFilePath));
        var openAssignment = (await repository.GetOpenAssignmentsAsync()).First();
        openAssignment.Status = AssignmentStatus.Completed;

        await repository.UpdateAsync(openAssignment);

        var reopenedRepository = new JsonAssignmentRepository(new HomeschoolDataStore(_dataFilePath));
        var openAssignments = await reopenedRepository.GetOpenAssignmentsAsync();

        Assert.DoesNotContain(openAssignments, a => a.Id == openAssignment.Id);
    }

    [Fact]
    public void DataStore_UsesLocalAppDataByDefault()
    {
        var configuration = new TestConfiguration();
        var store = new HomeschoolDataStore(configuration);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var expectedRoot = string.IsNullOrWhiteSpace(localAppData)
            ? AppContext.BaseDirectory
            : Path.Combine(localAppData, "HomeschoolManager");

        Assert.Equal(
            Path.GetFullPath(Path.Combine(expectedRoot, "homeschool-data.json")),
            store.FilePath);
    }

    [Fact]
    public void DataStore_UsesConfiguredStoragePath()
    {
        var customPath = Path.Combine(_testDirectory, "custom-data.json");
        var configuration = new TestConfiguration();
        configuration["DataStorage:FilePath"] = customPath;

        var store = new HomeschoolDataStore(configuration);

        Assert.Equal(Path.GetFullPath(customPath), store.FilePath);
    }

    [Fact]
    public async Task DataStore_ExportsAndImportsJsonBackups()
    {
        var originalStore = new HomeschoolDataStore(_dataFilePath);
        var originalRepository = new JsonStudentRepository(originalStore);
        var created = await originalRepository.AddAsync(new Student
        {
            FirstName = "Maya",
            LastName = "Wells",
            DateOfBirth = new DateTime(2014, 4, 12),
            GradeLevel = "5th",
            EnrollmentDate = DateTime.Today
        });

        var json = await originalStore.ExportJsonAsync();
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var importedPath = Path.Combine(_testDirectory, "imported-data.json");
        var importedStore = new HomeschoolDataStore(importedPath);
        await importedStore.ImportJsonAsync(stream);

        var importedRepository = new JsonStudentRepository(importedStore);
        var imported = await importedRepository.GetByIdAsync(created.Id);

        Assert.NotNull(imported);
        Assert.Equal("Maya", imported.FirstName);
        Assert.Equal("Wells", imported.LastName);
    }

    [Fact]
    public async Task DataStore_ExportIncludesSchemaVersion()
    {
        var store = new HomeschoolDataStore(_dataFilePath);

        var json = await store.ExportJsonAsync();
        await using var stream = StreamFromString(json);
        var preview = await store.PreviewImportJsonAsync(stream);

        Assert.Contains("\"schemaVersion\": 7", json);
        Assert.Equal(7, preview.SchemaVersion);
        Assert.Equal(3, preview.StudentCount);
        Assert.Equal(4, preview.CourseCount);
        Assert.Equal(0, preview.LessonPlanCount);
        Assert.Equal(3, preview.AttendanceRecordCount);
        Assert.Equal(2, preview.LearningTimeEntryCount);
        Assert.Equal(2, preview.PortfolioItemCount);
        Assert.Equal(2, preview.CurriculumResourceCount);
        Assert.Equal(2, preview.StudentCurriculumCount);
        Assert.Equal(2, preview.ParentNoteCount);
        Assert.Equal(0, preview.YearbookCount);
        Assert.Equal(0, preview.YearbookPageCount);
        Assert.Equal(0, preview.YearbookAssetCount);
    }

    [Fact]
    public async Task YearbookRepository_CreatesYearbookAndSavesPages()
    {
        var repository = new JsonYearbookRepository(new HomeschoolDataStore(_dataFilePath));
        var yearbook = await repository.AddAsync(new Yearbook
        {
            FamilyId = 1,
            Title = "Family Yearbook",
            SchoolYear = "2026-2027",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 6, 30),
            Scope = YearbookScope.Family
        });
        await repository.SavePagesAsync(yearbook.Id, new[]
        {
            new YearbookPage
            {
                YearbookId = yearbook.Id,
                Title = "Cover",
                SortOrder = 0,
                ContentJson = "{\"body\":\"Hello\"}",
                Elements = new List<PageElement>
                {
                    new()
                    {
                        Type = PageElementType.Text,
                        X = 10,
                        Y = 20,
                        Width = 300,
                        Height = 80,
                        Text = "Hello",
                        ZIndex = 1
                    }
                }
            },
            new YearbookPage { YearbookId = yearbook.Id, Title = "Closing", SortOrder = 1, ContentJson = "{\"body\":\"Bye\"}" }
        });

        var reloaded = await repository.GetByIdAsync(yearbook.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("Family Yearbook", reloaded.Title);
        Assert.Equal(2, reloaded.Pages.Count);
        Assert.Equal("Cover", reloaded.Pages[0].Title);
        Assert.Single(reloaded.Pages[0].Elements);
        Assert.Equal(PageElementType.Text, reloaded.Pages[0].Elements[0].Type);
        Assert.Equal("Hello", reloaded.Pages[0].Elements[0].Text);
    }

    [Fact]
    public async Task YearbookRepository_MigratesLegacyTextAndAssetsToPageElements()
    {
        var store = new HomeschoolDataStore(_dataFilePath);
        var portfolioRepository = new JsonPortfolioRepository(store);
        var photo = await portfolioRepository.AddAsync(new PortfolioItem
        {
            StudentId = 1,
            SubjectId = 1,
            Type = PortfolioItemType.Photo,
            Title = "Science Fair",
            Description = "Display board photo.",
            Date = new DateTime(2026, 5, 5),
            ExternalUrl = "https://example.com/science-fair.jpg"
        });
        var repository = new JsonYearbookRepository(store);
        var yearbook = await repository.AddAsync(new Yearbook
        {
            FamilyId = 1,
            Title = "Legacy Yearbook",
            SchoolYear = "2026-2027",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 6, 30),
            Scope = YearbookScope.Family
        });
        var page = await repository.AddPageAsync(new YearbookPage
        {
            YearbookId = yearbook.Id,
            Title = "Legacy Page",
            SortOrder = 0,
            ContentJson = "{\"body\":\"Legacy page text\"}"
        });
        await repository.SaveAssetsAsync(yearbook.Id, new[]
        {
            new YearbookAsset
            {
                YearbookId = yearbook.Id,
                YearbookPageId = page.Id,
                PortfolioItemId = photo.Id,
                Title = photo.Title,
                SourcePath = photo.ExternalUrl,
                Caption = photo.Description
            }
        });

        var reloaded = await repository.GetByIdAsync(yearbook.Id);
        var migratedPage = Assert.Single(reloaded!.Pages);

        Assert.Contains(migratedPage.Elements, e => e.Type == PageElementType.Text && e.Text == "Legacy page text");
        Assert.Contains(migratedPage.Elements, e => e.Type == PageElementType.Photo && e.Src == photo.ExternalUrl);
        Assert.Equal(migratedPage.Elements.Select(e => e.Id).Distinct().Count(), migratedPage.Elements.Count);

        YearbookPageMigration.EnsureElements(migratedPage, reloaded.Assets);

        Assert.Equal(migratedPage.Elements.Select(e => e.Id).Distinct().Count(), migratedPage.Elements.Count);
        Assert.Equal(2, migratedPage.Elements.Count);
    }

    [Fact]
    public async Task YearbookRepository_RejectsInvalidPageJson()
    {
        var repository = new JsonYearbookRepository(new HomeschoolDataStore(_dataFilePath));
        var yearbook = await repository.AddAsync(new Yearbook
        {
            FamilyId = 1,
            Title = "Validation Yearbook",
            SchoolYear = "2026-2027",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2027, 6, 30),
            Scope = YearbookScope.Family
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AddPageAsync(new YearbookPage
            {
                YearbookId = yearbook.Id,
                Title = "Broken",
                SortOrder = 0,
                ContentJson = "{not json"
            }));

        Assert.Contains("valid JSON", exception.Message);
    }

    [Fact]
    public async Task ParentNoteRepository_CreatesAndFiltersNotes()
    {
        var store = new HomeschoolDataStore(_dataFilePath);
        var lessonRepository = new JsonLessonPlanRepository(store);
        var lesson = await lessonRepository.AddAsync(new LessonPlan
        {
            FamilyId = 1,
            StudentId = 1,
            SubjectId = 1,
            Title = "Note Lesson",
            PlannedDate = new DateTime(2026, 5, 10),
            EstimatedMinutes = 30
        });
        var repository = new JsonParentNoteRepository(store);
        var note = await repository.AddAsync(new ParentNote
        {
            StudentId = 1,
            SubjectId = 1,
            AssignmentId = 1,
            LessonPlanId = lesson.Id,
            Category = ParentNoteCategory.Breakthrough,
            Title = "Multiplication breakthrough",
            Content = "Student explained the pattern independently.",
            NoteDate = new DateTime(2026, 5, 10)
        });

        var filtered = (await repository.GetFilteredAsync(new ParentNoteFilter(
            StudentId: 1,
            SubjectId: 1,
            AssignmentId: 1,
            LessonPlanId: lesson.Id,
            Category: ParentNoteCategory.Breakthrough,
            StartDate: new DateTime(2026, 5, 1),
            EndDate: new DateTime(2026, 5, 31)))).ToList();

        Assert.Contains(filtered, n => n.Id == note.Id);
        Assert.Equal("Math", filtered.First(n => n.Id == note.Id).Course?.Subject);
        Assert.Equal("Algebra Worksheet", filtered.First(n => n.Id == note.Id).Assignment?.Title);
        Assert.Equal("Note Lesson", filtered.First(n => n.Id == note.Id).LessonPlan?.Title);
    }

    [Fact]
    public async Task CurriculumResourceRepository_CreatesResources()
    {
        var repository = new JsonCurriculumResourceRepository(new HomeschoolDataStore(_dataFilePath));
        var created = await repository.AddAsync(new CurriculumResource
        {
            SubjectId = 3,
            Title = "Creative Writing Prompts",
            Description = "Daily writing prompts and revision checklists.",
            ResourceType = CurriculumResourceType.Workbook,
            Publisher = "Writing House",
            GradeLevel = "7th"
        });

        var reloaded = await repository.GetByIdAsync(created.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("Creative Writing Prompts", reloaded.Title);
        Assert.Equal("Language Arts", reloaded.Subject);
        Assert.Equal(CurriculumResourceType.Workbook, reloaded.ResourceType);
    }

    [Fact]
    public async Task StudentCurriculumRepository_AssignsResourceAndPreventsDuplicates()
    {
        var store = new HomeschoolDataStore(_dataFilePath);
        var resourceRepository = new JsonCurriculumResourceRepository(store);
        var studentCurriculumRepository = new JsonStudentCurriculumRepository(store);
        var resource = await resourceRepository.AddAsync(new CurriculumResource
        {
            SubjectId = 2,
            Title = "Ancient History Reader",
            ResourceType = CurriculumResourceType.Book
        });

        var assignment = await studentCurriculumRepository.AddAsync(new StudentCurriculum
        {
            StudentId = 1,
            CurriculumResourceId = resource.Id,
            Status = CurriculumStatus.NotStarted,
            StartDate = new DateTime(2026, 8, 1),
            TargetEndDate = new DateTime(2026, 12, 15)
        });

        var duplicate = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            studentCurriculumRepository.AddAsync(new StudentCurriculum
            {
                StudentId = 1,
                CurriculumResourceId = resource.Id
            }));
        var reloaded = await studentCurriculumRepository.GetByStudentAndResourceAsync(1, resource.Id);

        Assert.Contains("already assigned", duplicate.Message);
        Assert.NotNull(reloaded);
        Assert.Equal(assignment.Id, reloaded.Id);
        Assert.Equal("Ancient History Reader", reloaded.CurriculumResource.Title);
    }

    [Fact]
    public async Task StudentCurriculumRepository_UpdatesProgress()
    {
        var repository = new JsonStudentCurriculumRepository(new HomeschoolDataStore(_dataFilePath));
        var item = await repository.AddAsync(new StudentCurriculum
        {
            StudentId = 3,
            CurriculumResourceId = 1,
            Status = CurriculumStatus.NotStarted
        });

        item.Status = CurriculumStatus.InProgress;
        item.CurrentUnit = "Unit 4";
        item.CurrentLesson = "Quadratic patterns";
        item.PercentComplete = 55;
        await repository.UpdateAsync(item);

        var reloaded = await repository.GetByIdAsync(item.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(CurriculumStatus.InProgress, reloaded.Status);
        Assert.Equal("Unit 4", reloaded.CurrentUnit);
        Assert.Equal("Quadratic patterns", reloaded.CurrentLesson);
        Assert.Equal(55, reloaded.PercentComplete);
    }

    [Fact]
    public async Task StudentCurriculumRepository_RejectsInvalidProgress()
    {
        var repository = new JsonStudentCurriculumRepository(new HomeschoolDataStore(_dataFilePath));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AddAsync(new StudentCurriculum
            {
                StudentId = 3,
                CurriculumResourceId = 1,
                PercentComplete = 101
            }));

        Assert.Contains("between 0 and 100", exception.Message);
    }

    [Fact]
    public async Task PortfolioRepository_CreatesAndFiltersPortfolioItems()
    {
        var repository = new JsonPortfolioRepository(new HomeschoolDataStore(_dataFilePath));
        var item = await repository.AddAsync(new PortfolioItem
        {
            StudentId = 1,
            SubjectId = 1,
            Type = PortfolioItemType.Photo,
            Title = "Geometry Model",
            Description = "Photo of completed model.",
            Date = new DateTime(2026, 5, 5),
            IsBestWork = true,
            AssignmentId = 1,
            Tags = "geometry,model"
        });

        var filtered = (await repository.GetFilteredAsync(new PortfolioFilter(
            StudentId: 1,
            SubjectId: 1,
            Type: PortfolioItemType.Photo,
            StartDate: new DateTime(2026, 5, 1),
            EndDate: new DateTime(2026, 5, 31),
            BestWorkOnly: true))).ToList();

        Assert.Contains(filtered, i => i.Id == item.Id);
        Assert.DoesNotContain(filtered, i => i.StudentId != 1 || i.SubjectId != 1 || i.Type != PortfolioItemType.Photo);
    }

    [Fact]
    public async Task PortfolioRepository_ReturnsAssignmentAndLessonLinkedItems()
    {
        var store = new HomeschoolDataStore(_dataFilePath);
        var lessonRepository = new JsonLessonPlanRepository(store);
        var lesson = await lessonRepository.AddAsync(new LessonPlan
        {
            FamilyId = 1,
            StudentId = 1,
            SubjectId = 1,
            Title = "Portfolio Lesson",
            PlannedDate = new DateTime(2026, 5, 6),
            EstimatedMinutes = 30
        });
        var repository = new JsonPortfolioRepository(store);
        var item = await repository.AddAsync(new PortfolioItem
        {
            StudentId = 1,
            SubjectId = 1,
            Type = PortfolioItemType.Note,
            Title = "Lesson Reflection",
            Date = new DateTime(2026, 5, 6),
            AssignmentId = 1,
            LessonPlanId = lesson.Id
        });

        var assignmentItems = (await repository.GetByAssignmentIdAsync(1)).ToList();
        var lessonItems = (await repository.GetByLessonPlanIdAsync(lesson.Id)).ToList();

        Assert.Contains(assignmentItems, i => i.Id == item.Id);
        Assert.Contains(lessonItems, i => i.Id == item.Id);
    }

    [Fact]
    public async Task AttendanceRepository_PreventsDuplicateStudentDateRecords()
    {
        var repository = new JsonAttendanceRepository(new HomeschoolDataStore(_dataFilePath));
        var date = new DateTime(2026, 1, 8);
        await repository.AddAsync(new AttendanceRecord
        {
            StudentId = 1,
            Date = date,
            Status = AttendanceStatus.Present,
            Minutes = 240
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AddAsync(new AttendanceRecord
            {
                StudentId = 1,
                Date = date.AddHours(10),
                Status = AttendanceStatus.Absent
            }));

        Assert.Contains("already recorded", exception.Message);
    }

    [Fact]
    public async Task LessonPlanRepository_PersistsAndFiltersWeeklyLessons()
    {
        var repository = new JsonLessonPlanRepository(new HomeschoolDataStore(_dataFilePath));
        var plannedDate = new DateTime(2026, 5, 27);
        var created = await repository.AddAsync(new LessonPlan
        {
            FamilyId = 1,
            StudentId = 1,
            SubjectId = 1,
            Title = "Fractions",
            Description = "Practice equivalent fractions.",
            PlannedDate = plannedDate,
            EstimatedMinutes = 30
        });

        var reopenedRepository = new JsonLessonPlanRepository(new HomeschoolDataStore(_dataFilePath));
        var week = (await reopenedRepository.GetByWeekAsync(new DateTime(2026, 5, 25), studentId: 1, subjectId: 1)).ToList();

        Assert.Single(week);
        Assert.Equal(created.Id, week[0].Id);
        Assert.Equal("Fractions", week[0].Title);
    }

    [Fact]
    public async Task DataStore_RejectsImportWithDanglingReferences()
    {
        var store = new HomeschoolDataStore(_dataFilePath);
        const string json = """
            {
              "schemaVersion": 1,
              "students": [
                {
                  "id": 1,
                  "firstName": "Nora",
                  "lastName": "Stone",
                  "dateOfBirth": "2014-05-01T00:00:00",
                  "gradeLevel": "5th",
                  "enrollmentDate": "2024-08-01T00:00:00",
                  "createdAt": "2024-08-01T00:00:00"
                }
              ],
              "courses": [],
              "assignments": [
                {
                  "id": 1,
                  "title": "Reading",
                  "dueDate": "2024-09-01T00:00:00",
                  "assignedDate": "2024-08-25T00:00:00",
                  "courseId": 99,
                  "studentId": 1,
                  "createdAt": "2024-08-25T00:00:00"
                }
              ],
              "grades": []
            }
            """;

        await using var stream = StreamFromString(json);
        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => store.PreviewImportJsonAsync(stream));

        Assert.Contains("missing course 99", exception.Message);
    }

    [Fact]
    public async Task DataStore_ImportCreatesBackupOfPreviousData()
    {
        var targetStore = new HomeschoolDataStore(_dataFilePath);
        var targetRepository = new JsonStudentRepository(targetStore);
        await targetRepository.AddAsync(new Student
        {
            FirstName = "Taylor",
            LastName = "Original",
            DateOfBirth = new DateTime(2013, 9, 14),
            GradeLevel = "6th",
            EnrollmentDate = DateTime.Today
        });

        var sourcePath = Path.Combine(_testDirectory, "source-data.json");
        var sourceStore = new HomeschoolDataStore(sourcePath);
        var sourceRepository = new JsonStudentRepository(sourceStore);
        await sourceRepository.AddAsync(new Student
        {
            FirstName = "Morgan",
            LastName = "Imported",
            DateOfBirth = new DateTime(2012, 10, 3),
            GradeLevel = "7th",
            EnrollmentDate = DateTime.Today
        });

        var importJson = await sourceStore.ExportJsonAsync();
        await using var stream = StreamFromString(importJson);

        await targetStore.ImportJsonAsync(stream);

        Assert.True(File.Exists(targetStore.BackupFilePath));
        var backupJson = await File.ReadAllTextAsync(targetStore.BackupFilePath);
        Assert.Contains("Taylor", backupJson);

        var reloadedRepository = new JsonStudentRepository(new HomeschoolDataStore(_dataFilePath));
        var students = (await reloadedRepository.GetAllAsync()).ToList();
        Assert.Contains(students, s => s.FirstName == "Morgan" && s.LastName == "Imported");
        Assert.DoesNotContain(students, s => s.FirstName == "Taylor" && s.LastName == "Original");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private sealed class TestConfiguration : IConfiguration
    {
        private readonly Dictionary<string, string?> _values = new();

        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var value) ? value : null;
            set => _values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return Array.Empty<IConfigurationSection>();
        }

        public IChangeToken GetReloadToken()
        {
            return TestChangeToken.Instance;
        }

        public IConfigurationSection GetSection(string key)
        {
            return new TestConfigurationSection(key, this[key]);
        }
    }

    private sealed class TestConfigurationSection : IConfigurationSection
    {
        public TestConfigurationSection(string key, string? value)
        {
            Key = key;
            Path = key;
            Value = value;
        }

        public string? this[string key]
        {
            get => null;
            set { }
        }

        public string Key { get; }
        public string Path { get; }
        public string? Value { get; set; }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return Array.Empty<IConfigurationSection>();
        }

        public IChangeToken GetReloadToken()
        {
            return TestChangeToken.Instance;
        }

        public IConfigurationSection GetSection(string key)
        {
            return new TestConfigurationSection(key, null);
        }
    }

    private sealed class TestChangeToken : IChangeToken
    {
        public static readonly TestChangeToken Instance = new();

        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            return NoopDisposable.Instance;
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }

    private static MemoryStream StreamFromString(string value)
    {
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(value));
    }
}
