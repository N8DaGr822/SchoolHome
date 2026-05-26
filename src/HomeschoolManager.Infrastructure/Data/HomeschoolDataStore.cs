using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
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
    public string BackupFilePath => $"{_filePath}.bak";

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

    public async Task<HomeschoolImportPreview> PreviewImportJsonAsync(Stream stream)
    {
        var imported = await ParseImportAsync(stream);
        return CreatePreview(imported);
    }

    public async Task ImportJsonAsync(Stream stream)
    {
        var imported = await ParseImportAsync(stream);

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
        PrepareForStorage(_data);
        return _data;
    }

    private async Task SaveAsync(HomeschoolData data)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Validate(data);

        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, data, JsonOptions);
                await stream.FlushAsync();
            }

            if (File.Exists(_filePath))
            {
                if (File.Exists(BackupFilePath))
                {
                    File.Delete(BackupFilePath);
                }

                File.Replace(tempPath, _filePath, BackupFilePath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, _filePath);
            }
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
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
        if (data.SchemaVersion > HomeschoolData.CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"This backup uses schema version {data.SchemaVersion}, but this app supports version {HomeschoolData.CurrentSchemaVersion}.");
        }

        data.SchemaVersion = HomeschoolData.CurrentSchemaVersion;
        data.Students ??= new List<Student>();
        data.Courses ??= new List<Course>();
        data.Assignments ??= new List<Assignment>();
        data.Grades ??= new List<Grade>();

        foreach (var student in data.Students)
        {
            student.Courses ??= new List<Course>();
            student.Assignments ??= new List<Assignment>();
            student.Grades ??= new List<Grade>();
        }

        foreach (var course in data.Courses)
        {
            course.Students ??= new List<Student>();
            course.Assignments ??= new List<Assignment>();
            course.LessonPlans ??= new List<LessonPlan>();
            foreach (var lessonPlan in course.LessonPlans)
            {
                if (lessonPlan.CourseId == 0)
                {
                    lessonPlan.CourseId = course.Id;
                }
            }
        }

        foreach (var assignment in data.Assignments)
        {
            assignment.Grades ??= new List<Grade>();
        }
    }

    private static async Task<HomeschoolData> ParseImportAsync(Stream stream)
    {
        HomeschoolData? imported;
        try
        {
            imported = await JsonSerializer.DeserializeAsync<HomeschoolData>(stream, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The selected file is not valid homeschool backup JSON.", ex);
        }

        if (imported == null)
        {
            throw new InvalidDataException("The selected file does not contain homeschool data.");
        }

        PrepareForStorage(imported);
        return imported;
    }

    private static void PrepareForStorage(HomeschoolData data)
    {
        Normalize(data);
        SanitizeForStorage(data);
        Validate(data);
    }

    private static HomeschoolImportPreview CreatePreview(HomeschoolData data)
    {
        return new HomeschoolImportPreview(
            data.SchemaVersion,
            data.Students.Count,
            data.Courses.Count,
            data.Assignments.Count,
            data.Grades.Count);
    }

    private static void Validate(HomeschoolData data)
    {
        ValidateUniqueIds(data.Students.Select(s => s.Id), "Student");
        ValidateUniqueIds(data.Courses.Select(c => c.Id), "Course");
        ValidateUniqueIds(data.Assignments.Select(a => a.Id), "Assignment");
        ValidateUniqueIds(data.Grades.Select(g => g.Id), "Grade");

        foreach (var student in data.Students)
        {
            ValidateStudent(student);
        }

        foreach (var course in data.Courses)
        {
            ValidateObject(course, $"Course {course.Id}");
            ValidateUniqueIds(course.LessonPlans.Select(lp => lp.Id), $"Lesson plans for course {course.Id}");
            foreach (var lessonPlan in course.LessonPlans)
            {
                ValidateObject(lessonPlan, $"Lesson plan {lessonPlan.Id}");
                if (lessonPlan.CourseId != course.Id)
                {
                    throw new InvalidDataException($"Lesson plan {lessonPlan.Id} points to course {lessonPlan.CourseId}, but it is stored under course {course.Id}.");
                }
            }
        }

        var studentIds = data.Students.Select(s => s.Id).ToHashSet();
        var courseIds = data.Courses.Select(c => c.Id).ToHashSet();
        var assignmentIds = data.Assignments.Select(a => a.Id).ToHashSet();

        foreach (var assignment in data.Assignments)
        {
            ValidateObject(assignment, $"Assignment {assignment.Id}");
            if (!studentIds.Contains(assignment.StudentId))
            {
                throw new InvalidDataException($"Assignment {assignment.Id} points to missing student {assignment.StudentId}.");
            }

            if (!courseIds.Contains(assignment.CourseId))
            {
                throw new InvalidDataException($"Assignment {assignment.Id} points to missing course {assignment.CourseId}.");
            }
        }

        foreach (var grade in data.Grades)
        {
            if (grade.Id <= 0)
            {
                throw new InvalidDataException("Grade records must have a positive id.");
            }

            if (!studentIds.Contains(grade.StudentId))
            {
                throw new InvalidDataException($"Grade {grade.Id} points to missing student {grade.StudentId}.");
            }

            if (!assignmentIds.Contains(grade.AssignmentId))
            {
                throw new InvalidDataException($"Grade {grade.Id} points to missing assignment {grade.AssignmentId}.");
            }
        }
    }

    private static void ValidateUniqueIds(IEnumerable<int> ids, string label)
    {
        var seen = new HashSet<int>();
        foreach (var id in ids)
        {
            if (id <= 0)
            {
                throw new InvalidDataException($"{label} records must have positive ids.");
            }

            if (!seen.Add(id))
            {
                throw new InvalidDataException($"{label} id {id} is duplicated.");
            }
        }
    }

    private static void ValidateObject(object instance, string label)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance);
        if (!Validator.TryValidateObject(instance, context, results, validateAllProperties: true))
        {
            var message = results.FirstOrDefault()?.ErrorMessage ?? "The record is invalid.";
            throw new InvalidDataException($"{label}: {message}");
        }
    }

    private static void ValidateStudent(Student student)
    {
        var value = Clone(student);
        if (string.IsNullOrWhiteSpace(value.Email))
        {
            value.Email = "optional@example.com";
        }

        ValidateObject(value, $"Student {student.Id}");
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
