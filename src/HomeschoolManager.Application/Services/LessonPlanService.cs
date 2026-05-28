using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Application.Services;

public class LessonPlanService : ILessonPlanService
{
    private readonly ILessonPlanRepository _lessonPlanRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ILearningTimeService? _learningTimeService;

    public LessonPlanService(
        ILessonPlanRepository lessonPlanRepository,
        IStudentRepository studentRepository,
        IRepository<Course> courseRepository,
        IAssignmentRepository assignmentRepository,
        ILearningTimeService? learningTimeService = null)
    {
        _lessonPlanRepository = lessonPlanRepository;
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _assignmentRepository = assignmentRepository;
        _learningTimeService = learningTimeService;
    }

    public async Task<LessonPlan?> GetLessonPlanByIdAsync(int id)
    {
        return await _lessonPlanRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<LessonPlan>> GetAllLessonPlansAsync()
    {
        return await _lessonPlanRepository.GetAllAsync();
    }

    public async Task<IEnumerable<LessonPlan>> GetWeeklyLessonPlansAsync(DateTime weekStart, int? studentId = null, int? subjectId = null)
    {
        return await _lessonPlanRepository.GetByWeekAsync(GetWeekStart(weekStart), studentId, subjectId);
    }

    public async Task<LessonPlan> CreateLessonPlanAsync(LessonPlan lessonPlan)
    {
        await ValidateLessonPlanAsync(lessonPlan);
        lessonPlan.FamilyId = lessonPlan.FamilyId == 0 ? 1 : lessonPlan.FamilyId;
        lessonPlan.CourseId = lessonPlan.SubjectId;
        lessonPlan.DurationMinutes = lessonPlan.EstimatedMinutes;
        lessonPlan.WeekNumber = lessonPlan.WeekNumber == 0 ? 1 : lessonPlan.WeekNumber;
        lessonPlan.DayNumber = lessonPlan.DayNumber == 0 ? Math.Max(1, (int)lessonPlan.PlannedDate.DayOfWeek) : lessonPlan.DayNumber;
        lessonPlan.CreatedAt = DateTime.UtcNow;
        return await _lessonPlanRepository.AddAsync(lessonPlan);
    }

    public async Task<LessonPlan> UpdateLessonPlanAsync(LessonPlan lessonPlan)
    {
        await ValidateLessonPlanAsync(lessonPlan);
        lessonPlan.FamilyId = lessonPlan.FamilyId == 0 ? 1 : lessonPlan.FamilyId;
        lessonPlan.CourseId = lessonPlan.SubjectId;
        lessonPlan.DurationMinutes = lessonPlan.EstimatedMinutes;
        lessonPlan.WeekNumber = lessonPlan.WeekNumber == 0 ? 1 : lessonPlan.WeekNumber;
        lessonPlan.DayNumber = lessonPlan.DayNumber == 0 ? Math.Max(1, (int)lessonPlan.PlannedDate.DayOfWeek) : lessonPlan.DayNumber;
        lessonPlan.UpdatedAt = DateTime.UtcNow;
        await _lessonPlanRepository.UpdateAsync(lessonPlan);
        return lessonPlan;
    }

    public async Task DeleteLessonPlanAsync(int id)
    {
        await _lessonPlanRepository.DeleteAsync(id);
    }

    public async Task<LessonPlan> CompleteLessonPlanAsync(int id, bool createLearningTimeEntry = false)
    {
        var lessonPlan = await GetRequiredLessonPlanAsync(id);
        lessonPlan.Status = LessonPlanStatus.Completed;
        var updated = await UpdateLessonPlanAsync(lessonPlan);
        if (createLearningTimeEntry && _learningTimeService is not null)
        {
            await _learningTimeService.CreateFromLessonCompletionAsync(updated);
        }

        return updated;
    }

    public async Task<LessonPlan> SkipLessonPlanAsync(int id)
    {
        var lessonPlan = await GetRequiredLessonPlanAsync(id);
        lessonPlan.Status = LessonPlanStatus.Skipped;
        return await UpdateLessonPlanAsync(lessonPlan);
    }

    public async Task<LessonPlan> MoveLessonPlanAsync(int id, DateTime plannedDate)
    {
        var lessonPlan = await GetRequiredLessonPlanAsync(id);
        lessonPlan.PlannedDate = plannedDate.Date;
        return await UpdateLessonPlanAsync(lessonPlan);
    }

    public async Task<Assignment> ConvertToAssignmentAsync(int id)
    {
        var lessonPlan = await GetRequiredLessonPlanAsync(id);
        if (lessonPlan.AssignmentId.HasValue)
        {
            return await _assignmentRepository.GetByIdAsync(lessonPlan.AssignmentId.Value)
                ?? throw new InvalidOperationException($"Assignment {lessonPlan.AssignmentId.Value} was not found.");
        }

        await ValidateLessonPlanAsync(lessonPlan);
        var course = await _courseRepository.GetByIdAsync(lessonPlan.SubjectId)
            ?? throw new InvalidOperationException($"Subject {lessonPlan.SubjectId} was not found.");

        var assignment = await _assignmentRepository.AddAsync(new Assignment
        {
            Title = lessonPlan.Title,
            Description = lessonPlan.Description,
            DueDate = lessonPlan.PlannedDate.Date,
            AssignedDate = DateTime.Today,
            Status = AssignmentStatus.Assigned,
            CourseId = lessonPlan.SubjectId,
            StudentId = lessonPlan.StudentId,
            Subject = string.IsNullOrWhiteSpace(course.Subject) ? course.Name : course.Subject,
            EstimatedMinutes = lessonPlan.EstimatedMinutes,
            CreatedAt = DateTime.UtcNow
        });

        lessonPlan.AssignmentId = assignment.Id;
        await UpdateLessonPlanAsync(lessonPlan);
        return assignment;
    }

    private async Task ValidateLessonPlanAsync(LessonPlan lessonPlan)
    {
        if (lessonPlan.FamilyId < 0)
        {
            throw new InvalidOperationException("Family id cannot be negative.");
        }

        if (lessonPlan.StudentId <= 0 || !await _studentRepository.ExistsAsync(lessonPlan.StudentId))
        {
            throw new InvalidOperationException("A valid student is required.");
        }

        if (lessonPlan.SubjectId <= 0 || !await _courseRepository.ExistsAsync(lessonPlan.SubjectId))
        {
            throw new InvalidOperationException("A valid subject is required.");
        }

        if (string.IsNullOrWhiteSpace(lessonPlan.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (lessonPlan.EstimatedMinutes <= 0)
        {
            throw new InvalidOperationException("Estimated minutes must be greater than zero.");
        }
    }

    private async Task<LessonPlan> GetRequiredLessonPlanAsync(int id)
    {
        return await _lessonPlanRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Lesson plan {id} was not found.");
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.Date.AddDays(-offset);
    }
}
