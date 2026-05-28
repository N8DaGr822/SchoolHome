using Microsoft.Extensions.DependencyInjection;
using HomeschoolManager.Application.Services;

namespace HomeschoolManager.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services here
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<ILessonPlanService, LessonPlanService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ILearningTimeService, LearningTimeService>();

        return services;
    }
}
