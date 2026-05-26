using System.Text.Json;
using System.Text.Json.Serialization;
using HomeschoolManager.Core.Entities;
using Microsoft.Extensions.Configuration;

namespace HomeschoolManager.Infrastructure.Data;

public sealed class HomeschoolDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;
    private HomeschoolData? _data;

    public string FilePath => _filePath;

    public HomeschoolDataStore(IConfiguration configuration)
    {
        _filePath = Path.GetFullPath(ResolveDataFilePath(configuration));
    }

    public HomeschoolDataStore(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);
    }

    public async Task<string> ExportJsonAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var data = await LoadAsync();
            return JsonSerializer.Serialize(data, JsonOptions);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ImportJsonAsync(Stream stream)
    {
        var imported = await JsonSerializer.DeserializeAsync<HomeschoolData>(stream, JsonOptions)
            ?? throw new InvalidOperationException("The selected file does not contain homeschool data.");

        Normalize(imported);
        SanitizeForStorage(imported);

        await _gate.WaitAsync();
        try
        {
            _data = imported;
            await SaveAsync(_data);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task<HomeschoolData> ReadAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var data = await LoadAsync();
            return Clone(data);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task WriteAsync(Action<HomeschoolData> update)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await LoadAsync();
            update(data);
            SanitizeForStorage(data);
            await SaveAsync(data);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal static T Clone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException("Unable to clone homeschool data.");
    }

    private async Task<HomeschoolData> LoadAsync()
    {
        if (_data != null)
        {
            return _data;
        }

        if (!File.Exists(_filePath))
        {
            _data = SeedData.Create();
            await SaveAsync(_data);
            return _data;
        }

        await using var stream = File.OpenRead(_filePath);
        _data = await JsonSerializer.DeserializeAsync<HomeschoolData>(stream, JsonOptions) ?? new HomeschoolData();
        Normalize(_data);
        SanitizeForStorage(_data);
        return _data;
    }

    private async Task SaveAsync(HomeschoolData data)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions);
    }

    private static string ResolveDataFilePath(IConfiguration configuration)
    {
        var configuredPath = configuration["DataStorage:FilePath"] ?? configuration["DataFilePath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var storageRoot = string.IsNullOrWhiteSpace(localAppData)
            ? AppContext.BaseDirectory
            : Path.Combine(localAppData, "HomeschoolManager");

        return Path.Combine(storageRoot, "homeschool-data.json");
    }

    private static void Normalize(HomeschoolData data)
    {
        data.Students ??= new List<Student>();
        data.Courses ??= new List<Course>();
        data.Assignments ??= new List<Assignment>();
        data.Grades ??= new List<Grade>();
    }

    private static void SanitizeForStorage(HomeschoolData data)
    {
        foreach (var student in data.Students)
        {
            student.Courses = new List<Course>();
            student.Assignments = new List<Assignment>();
            student.Grades = new List<Grade>();
        }

        foreach (var course in data.Courses)
        {
            course.Students = new List<Student>();
            course.Assignments = new List<Assignment>();
            foreach (var lessonPlan in course.LessonPlans)
            {
                lessonPlan.Course = null!;
            }
        }

        foreach (var assignment in data.Assignments)
        {
            assignment.Course = null!;
            assignment.Student = null!;
            assignment.Grades = new List<Grade>();
        }

        foreach (var grade in data.Grades)
        {
            grade.AssignmentEntity = null!;
            grade.Student = null!;
        }
    }
}
