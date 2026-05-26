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

    public HomeschoolDataStore(IConfiguration configuration)
    {
        var configuredPath = configuration["DataFilePath"] ?? Path.Combine("App_Data", "homeschool-data.json");
        _filePath = Path.GetFullPath(configuredPath);
    }

    public HomeschoolDataStore(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);
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
