using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using HomeschoolManager.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
    private readonly ILogger<HomeschoolDataStore> _logger;
    private HomeschoolData? _data;

    public string FilePath => _filePath;
    public string BackupFilePath => $"{_filePath}.bak";

    public HomeschoolDataStore(IOptions<StorageOptions> options, ILogger<HomeschoolDataStore>? logger = null)
    {
        _filePath = Path.GetFullPath(options.Value.ResolveFilePath());
        _logger = logger ?? NullLogger<HomeschoolDataStore>.Instance;
    }

    public HomeschoolDataStore(string filePath, ILogger<HomeschoolDataStore>? logger = null)
    {
        _filePath = Path.GetFullPath(filePath);
        _logger = logger ?? NullLogger<HomeschoolDataStore>.Instance;
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
            _logger.LogInformation(
                "Imported homeschool backup with {StudentCount} student(s) and {CourseCount} course(s) into {FilePath}.",
                imported.Students.Count,
                imported.Courses.Count,
                _filePath);
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
            _logger.LogInformation("No data file found at {FilePath}; creating seed data.", _filePath);
            _data = SeedData.Create();
            await SaveAsync(_data);
            return _data;
        }

        await using var stream = File.OpenRead(_filePath);
        _data = await JsonSerializer.DeserializeAsync<HomeschoolData>(stream, JsonOptions) ?? new HomeschoolData();
        PrepareForStorage(_data);
        _logger.LogInformation("Loaded homeschool data from {FilePath}.", _filePath);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save homeschool data to {FilePath}.", _filePath);
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
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
        data.LessonPlans ??= new List<LessonPlan>();
        data.Assignments ??= new List<Assignment>();
        data.Grades ??= new List<Grade>();
        data.AttendanceRecords ??= new List<AttendanceRecord>();
        data.LearningTimeEntries ??= new List<LearningTimeEntry>();
        data.PortfolioItems ??= new List<PortfolioItem>();
        data.CurriculumResources ??= new List<CurriculumResource>();
        data.StudentCurricula ??= new List<StudentCurriculum>();
        data.ParentNotes ??= new List<ParentNote>();
        data.Yearbooks ??= new List<Yearbook>();
        data.YearbookPages ??= new List<YearbookPage>();
        data.YearbookAssets ??= new List<YearbookAsset>();

        foreach (var student in data.Students)
        {
            student.Courses ??= new List<Course>();
            student.Assignments ??= new List<Assignment>();
            student.Grades ??= new List<Grade>();
            student.AttendanceRecords ??= new List<AttendanceRecord>();
            student.LearningTimeEntries ??= new List<LearningTimeEntry>();
            student.PortfolioItems ??= new List<PortfolioItem>();
            student.StudentCurricula ??= new List<StudentCurriculum>();
            student.ParentNotes ??= new List<ParentNote>();
        }

        foreach (var course in data.Courses)
        {
            course.Students ??= new List<Student>();
            course.Assignments ??= new List<Assignment>();
            course.LessonPlans ??= new List<LessonPlan>();
            course.CurriculumResources ??= new List<CurriculumResource>();
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

        foreach (var attendanceRecord in data.AttendanceRecords)
        {
            attendanceRecord.Date = attendanceRecord.Date.Date;
            attendanceRecord.Notes ??= string.Empty;
        }

        foreach (var learningTimeEntry in data.LearningTimeEntries)
        {
            learningTimeEntry.Date = learningTimeEntry.Date.Date;
            learningTimeEntry.Subject ??= string.Empty;
            learningTimeEntry.Notes ??= string.Empty;
        }

        foreach (var portfolioItem in data.PortfolioItems)
        {
            portfolioItem.Date = portfolioItem.Date.Date;
            portfolioItem.Description ??= string.Empty;
            portfolioItem.Notes ??= string.Empty;
            portfolioItem.Subject ??= string.Empty;
            portfolioItem.ExternalUrl ??= string.Empty;
            portfolioItem.OriginalFileName ??= string.Empty;
            portfolioItem.StoredFileName ??= string.Empty;
            portfolioItem.StoredFilePath ??= string.Empty;
            portfolioItem.ContentType ??= string.Empty;
            portfolioItem.Tags ??= string.Empty;
        }

        foreach (var resource in data.CurriculumResources)
        {
            resource.Title = resource.Title?.Trim() ?? string.Empty;
            resource.Description = resource.Description?.Trim() ?? string.Empty;
            resource.Subject = resource.Subject?.Trim() ?? string.Empty;
            resource.Publisher = resource.Publisher?.Trim() ?? string.Empty;
            resource.Author = resource.Author?.Trim() ?? string.Empty;
            resource.Url = resource.Url?.Trim() ?? string.Empty;
            resource.GradeLevel = resource.GradeLevel?.Trim() ?? string.Empty;
            resource.StudentCurricula ??= new List<StudentCurriculum>();
        }

        foreach (var studentCurriculum in data.StudentCurricula)
        {
            studentCurriculum.CurrentUnit = studentCurriculum.CurrentUnit?.Trim() ?? string.Empty;
            studentCurriculum.CurrentLesson = studentCurriculum.CurrentLesson?.Trim() ?? string.Empty;
            studentCurriculum.StartDate = studentCurriculum.StartDate?.Date;
            studentCurriculum.TargetEndDate = studentCurriculum.TargetEndDate?.Date;
        }

        foreach (var parentNote in data.ParentNotes)
        {
            parentNote.Title = parentNote.Title?.Trim() ?? string.Empty;
            parentNote.Content = parentNote.Content?.Trim() ?? string.Empty;
            parentNote.NoteDate = parentNote.NoteDate.Date;
        }

        foreach (var yearbook in data.Yearbooks)
        {
            yearbook.Title = yearbook.Title?.Trim() ?? string.Empty;
            yearbook.SchoolYear = yearbook.SchoolYear?.Trim() ?? string.Empty;
            yearbook.StartDate = yearbook.StartDate.Date;
            yearbook.EndDate = yearbook.EndDate.Date;
            yearbook.Pages ??= new List<YearbookPage>();
            yearbook.Assets ??= new List<YearbookAsset>();
        }

        foreach (var page in data.YearbookPages)
        {
            page.Title = page.Title?.Trim() ?? string.Empty;
            page.ContentJson = string.IsNullOrWhiteSpace(page.ContentJson) ? "{}" : page.ContentJson.Trim();
            EnsureYearbookPageElementsInitialized(page);
        }

        foreach (var asset in data.YearbookAssets)
        {
            asset.Title = asset.Title?.Trim() ?? string.Empty;
            asset.SourcePath = asset.SourcePath?.Trim() ?? string.Empty;
            asset.Caption = asset.Caption?.Trim() ?? string.Empty;
        }

        foreach (var lessonPlan in data.LessonPlans)
        {
            if (lessonPlan.FamilyId == 0)
            {
                lessonPlan.FamilyId = 1;
            }

            if (lessonPlan.EstimatedMinutes == 0)
            {
                lessonPlan.EstimatedMinutes = 30;
            }

            if (lessonPlan.DurationMinutes == 0)
            {
                lessonPlan.DurationMinutes = lessonPlan.EstimatedMinutes;
            }

            if (lessonPlan.WeekNumber == 0)
            {
                lessonPlan.WeekNumber = 1;
            }

            if (lessonPlan.DayNumber == 0)
            {
                lessonPlan.DayNumber = Math.Max(1, (int)lessonPlan.PlannedDate.DayOfWeek);
            }
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
            data.LessonPlans.Count,
            data.Assignments.Count,
            data.Grades.Count,
            data.AttendanceRecords.Count,
            data.LearningTimeEntries.Count,
            data.PortfolioItems.Count,
            data.CurriculumResources.Count,
            data.StudentCurricula.Count,
            data.ParentNotes.Count,
            data.Yearbooks.Count,
            data.YearbookPages.Count,
            data.YearbookAssets.Count);
    }

    private static void Validate(HomeschoolData data)
    {
        ValidateUniqueIds(data.Students.Select(s => s.Id), "Student");
        ValidateUniqueIds(data.Courses.Select(c => c.Id), "Course");
        ValidateUniqueIds(data.LessonPlans.Select(lp => lp.Id), "Lesson plan");
        ValidateUniqueIds(data.Assignments.Select(a => a.Id), "Assignment");
        ValidateUniqueIds(data.Grades.Select(g => g.Id), "Grade");
        ValidateUniqueIds(data.AttendanceRecords.Select(a => a.Id), "Attendance");
        ValidateUniqueIds(data.LearningTimeEntries.Select(e => e.Id), "Learning time");
        ValidateUniqueIds(data.PortfolioItems.Select(i => i.Id), "Portfolio item");
        ValidateUniqueIds(data.CurriculumResources.Select(r => r.Id), "Curriculum resource");
        ValidateUniqueIds(data.StudentCurricula.Select(c => c.Id), "Student curriculum");
        ValidateUniqueIds(data.ParentNotes.Select(n => n.Id), "Parent note");
        ValidateUniqueIds(data.Yearbooks.Select(y => y.Id), "Yearbook");
        ValidateUniqueIds(data.YearbookPages.Select(p => p.Id), "Yearbook page");
        ValidateUniqueIds(data.YearbookAssets.Select(a => a.Id), "Yearbook asset");

        foreach (var student in data.Students)
        {
            ValidateObject(student, $"Student {student.Id}");
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
        var curriculumResourceIds = data.CurriculumResources.Select(r => r.Id).ToHashSet();

        foreach (var lessonPlan in data.LessonPlans)
        {
            ValidateObject(lessonPlan, $"Lesson plan {lessonPlan.Id}");
            if (lessonPlan.FamilyId <= 0)
            {
                throw new InvalidDataException($"Lesson plan {lessonPlan.Id} must have a family id.");
            }

            if (!studentIds.Contains(lessonPlan.StudentId))
            {
                throw new InvalidDataException($"Lesson plan {lessonPlan.Id} points to missing student {lessonPlan.StudentId}.");
            }

            if (!courseIds.Contains(lessonPlan.SubjectId))
            {
                throw new InvalidDataException($"Lesson plan {lessonPlan.Id} points to missing subject {lessonPlan.SubjectId}.");
            }

            if (lessonPlan.AssignmentId.HasValue && !assignmentIds.Contains(lessonPlan.AssignmentId.Value))
            {
                throw new InvalidDataException($"Lesson plan {lessonPlan.Id} points to missing assignment {lessonPlan.AssignmentId.Value}.");
            }
        }

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

        var attendanceKeys = new HashSet<(int StudentId, DateTime Date)>();
        foreach (var attendanceRecord in data.AttendanceRecords)
        {
            ValidateObject(attendanceRecord, $"Attendance {attendanceRecord.Id}");
            if (!studentIds.Contains(attendanceRecord.StudentId))
            {
                throw new InvalidDataException($"Attendance {attendanceRecord.Id} points to missing student {attendanceRecord.StudentId}.");
            }

            if (!attendanceKeys.Add((attendanceRecord.StudentId, attendanceRecord.Date.Date)))
            {
                throw new InvalidDataException($"Attendance for student {attendanceRecord.StudentId} on {attendanceRecord.Date:yyyy-MM-dd} is duplicated.");
            }
        }

        foreach (var learningTimeEntry in data.LearningTimeEntries)
        {
            ValidateObject(learningTimeEntry, $"Learning time {learningTimeEntry.Id}");
            if (!studentIds.Contains(learningTimeEntry.StudentId))
            {
                throw new InvalidDataException($"Learning time {learningTimeEntry.Id} points to missing student {learningTimeEntry.StudentId}.");
            }

            if (!courseIds.Contains(learningTimeEntry.SubjectId))
            {
                throw new InvalidDataException($"Learning time {learningTimeEntry.Id} points to missing subject {learningTimeEntry.SubjectId}.");
            }
        }

        foreach (var portfolioItem in data.PortfolioItems)
        {
            ValidateObject(portfolioItem, $"Portfolio item {portfolioItem.Id}");
            if (!studentIds.Contains(portfolioItem.StudentId))
            {
                throw new InvalidDataException($"Portfolio item {portfolioItem.Id} points to missing student {portfolioItem.StudentId}.");
            }

            if (!courseIds.Contains(portfolioItem.SubjectId))
            {
                throw new InvalidDataException($"Portfolio item {portfolioItem.Id} points to missing subject {portfolioItem.SubjectId}.");
            }

            if (portfolioItem.AssignmentId.HasValue && !assignmentIds.Contains(portfolioItem.AssignmentId.Value))
            {
                throw new InvalidDataException($"Portfolio item {portfolioItem.Id} points to missing assignment {portfolioItem.AssignmentId.Value}.");
            }

            if (portfolioItem.LessonPlanId.HasValue && !data.LessonPlans.Any(lp => lp.Id == portfolioItem.LessonPlanId.Value))
            {
                throw new InvalidDataException($"Portfolio item {portfolioItem.Id} points to missing lesson plan {portfolioItem.LessonPlanId.Value}.");
            }
        }

        foreach (var resource in data.CurriculumResources)
        {
            ValidateObject(resource, $"Curriculum resource {resource.Id}");
            if (!courseIds.Contains(resource.SubjectId))
            {
                throw new InvalidDataException($"Curriculum resource {resource.Id} points to missing subject {resource.SubjectId}.");
            }
        }

        var studentCurriculumKeys = new HashSet<(int StudentId, int ResourceId)>();
        foreach (var studentCurriculum in data.StudentCurricula)
        {
            ValidateObject(studentCurriculum, $"Student curriculum {studentCurriculum.Id}");
            if (!studentIds.Contains(studentCurriculum.StudentId))
            {
                throw new InvalidDataException($"Student curriculum {studentCurriculum.Id} points to missing student {studentCurriculum.StudentId}.");
            }

            if (!curriculumResourceIds.Contains(studentCurriculum.CurriculumResourceId))
            {
                throw new InvalidDataException($"Student curriculum {studentCurriculum.Id} points to missing curriculum resource {studentCurriculum.CurriculumResourceId}.");
            }

            if (!studentCurriculumKeys.Add((studentCurriculum.StudentId, studentCurriculum.CurriculumResourceId)))
            {
                throw new InvalidDataException($"Curriculum resource {studentCurriculum.CurriculumResourceId} is already assigned to student {studentCurriculum.StudentId}.");
            }
        }

        foreach (var parentNote in data.ParentNotes)
        {
            ValidateObject(parentNote, $"Parent note {parentNote.Id}");
            if (!studentIds.Contains(parentNote.StudentId))
            {
                throw new InvalidDataException($"Parent note {parentNote.Id} points to missing student {parentNote.StudentId}.");
            }

            if (parentNote.SubjectId.HasValue && !courseIds.Contains(parentNote.SubjectId.Value))
            {
                throw new InvalidDataException($"Parent note {parentNote.Id} points to missing subject {parentNote.SubjectId.Value}.");
            }

            if (parentNote.AssignmentId.HasValue && !assignmentIds.Contains(parentNote.AssignmentId.Value))
            {
                throw new InvalidDataException($"Parent note {parentNote.Id} points to missing assignment {parentNote.AssignmentId.Value}.");
            }

            if (parentNote.AssignmentId.HasValue && data.Assignments.First(a => a.Id == parentNote.AssignmentId.Value).StudentId != parentNote.StudentId)
            {
                throw new InvalidDataException($"Parent note {parentNote.Id} points to an assignment for a different student.");
            }

            if (parentNote.LessonPlanId.HasValue && !data.LessonPlans.Any(lp => lp.Id == parentNote.LessonPlanId.Value))
            {
                throw new InvalidDataException($"Parent note {parentNote.Id} points to missing lesson plan {parentNote.LessonPlanId.Value}.");
            }

            if (parentNote.LessonPlanId.HasValue && data.LessonPlans.First(lp => lp.Id == parentNote.LessonPlanId.Value).StudentId != parentNote.StudentId)
            {
                throw new InvalidDataException($"Parent note {parentNote.Id} points to a lesson plan for a different student.");
            }
        }

        var yearbookIds = data.Yearbooks.Select(y => y.Id).ToHashSet();
        var yearbookPageIds = data.YearbookPages.Select(p => p.Id).ToHashSet();
        var portfolioItemIds = data.PortfolioItems.Select(i => i.Id).ToHashSet();

        foreach (var yearbook in data.Yearbooks)
        {
            ValidateObject(yearbook, $"Yearbook {yearbook.Id}");
            if (yearbook.EndDate.Date < yearbook.StartDate.Date)
            {
                throw new InvalidDataException($"Yearbook {yearbook.Id} end date must be on or after start date.");
            }

            if (yearbook.Scope == YearbookScope.Student && (!yearbook.StudentId.HasValue || !studentIds.Contains(yearbook.StudentId.Value)))
            {
                throw new InvalidDataException($"Yearbook {yearbook.Id} requires a valid student.");
            }
        }

        foreach (var page in data.YearbookPages)
        {
            ValidateObject(page, $"Yearbook page {page.Id}");
            if (!yearbookIds.Contains(page.YearbookId))
            {
                throw new InvalidDataException($"Yearbook page {page.Id} points to missing yearbook {page.YearbookId}.");
            }

            if (!IsValidJson(page.ContentJson))
            {
                throw new InvalidDataException($"Yearbook page {page.Id} content must be valid JSON.");
            }
        }

        foreach (var asset in data.YearbookAssets)
        {
            ValidateObject(asset, $"Yearbook asset {asset.Id}");
            if (!yearbookIds.Contains(asset.YearbookId))
            {
                throw new InvalidDataException($"Yearbook asset {asset.Id} points to missing yearbook {asset.YearbookId}.");
            }

            if (asset.YearbookPageId.HasValue && !yearbookPageIds.Contains(asset.YearbookPageId.Value))
            {
                throw new InvalidDataException($"Yearbook asset {asset.Id} points to missing yearbook page {asset.YearbookPageId.Value}.");
            }

            if (asset.PortfolioItemId.HasValue && !portfolioItemIds.Contains(asset.PortfolioItemId.Value))
            {
                throw new InvalidDataException($"Yearbook asset {asset.Id} points to missing portfolio item {asset.PortfolioItemId.Value}.");
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

    private static void SanitizeForStorage(HomeschoolData data)
    {
        foreach (var student in data.Students)
        {
            student.Courses = new List<Course>();
            student.Assignments = new List<Assignment>();
            student.Grades = new List<Grade>();
            student.AttendanceRecords = new List<AttendanceRecord>();
            student.LearningTimeEntries = new List<LearningTimeEntry>();
            student.PortfolioItems = new List<PortfolioItem>();
            student.StudentCurricula = new List<StudentCurriculum>();
            student.ParentNotes = new List<ParentNote>();
        }

        foreach (var course in data.Courses)
        {
            course.Students = new List<Student>();
            course.Assignments = new List<Assignment>();
            course.CurriculumResources = new List<CurriculumResource>();
            foreach (var lessonPlan in course.LessonPlans)
            {
                lessonPlan.Course = null!;
            }
        }

        foreach (var lessonPlan in data.LessonPlans)
        {
            lessonPlan.Course = null!;
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

        foreach (var attendanceRecord in data.AttendanceRecords)
        {
            attendanceRecord.Student = null!;
        }

        foreach (var learningTimeEntry in data.LearningTimeEntries)
        {
            learningTimeEntry.Student = null!;
            learningTimeEntry.Course = null!;
        }

        foreach (var portfolioItem in data.PortfolioItems)
        {
            portfolioItem.Student = null!;
            portfolioItem.Course = null!;
            portfolioItem.Assignment = null;
            portfolioItem.LessonPlan = null;
        }

        foreach (var resource in data.CurriculumResources)
        {
            resource.Course = null!;
            resource.StudentCurricula = new List<StudentCurriculum>();
        }

        foreach (var studentCurriculum in data.StudentCurricula)
        {
            studentCurriculum.Student = null!;
            studentCurriculum.CurriculumResource = null!;
        }

        foreach (var parentNote in data.ParentNotes)
        {
            parentNote.Student = null!;
            parentNote.Course = null;
            parentNote.Assignment = null;
            parentNote.LessonPlan = null;
        }

        foreach (var yearbook in data.Yearbooks)
        {
            yearbook.Student = null;
            yearbook.Pages = new List<YearbookPage>();
            yearbook.Assets = new List<YearbookAsset>();
        }

        foreach (var page in data.YearbookPages)
        {
            EnsureYearbookPageElementsInitialized(page);
            page.Yearbook = null!;
        }

        foreach (var asset in data.YearbookAssets)
        {
            asset.Yearbook = null!;
            asset.Page = null;
            asset.PortfolioItem = null;
        }
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void EnsureYearbookPageElementsInitialized(YearbookPage page)
    {
        YearbookPageMigration.EnsureElements(page);
    }
}
