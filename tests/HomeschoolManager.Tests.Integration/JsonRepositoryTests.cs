using HomeschoolManager.Core.Entities;
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

        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Equal(1, preview.SchemaVersion);
        Assert.Equal(3, preview.StudentCount);
        Assert.Equal(4, preview.CourseCount);
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
