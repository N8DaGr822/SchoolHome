using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HomeschoolManager.Core.Entities;
using HomeschoolManager.Core.Interfaces;
using HomeschoolManager.Infrastructure.Data;
using HomeschoolManager.Infrastructure.Repositories;

namespace HomeschoolManager.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<HomeschoolDataStore>();
        services.AddScoped<IStudentRepository, JsonStudentRepository>();
        services.AddScoped<IRepository<Course>, JsonCourseRepository>();
        services.AddScoped<IAssignmentRepository, JsonAssignmentRepository>();
        services.AddScoped<ILessonPlanRepository, JsonLessonPlanRepository>();
        services.AddScoped<IAttendanceRepository, JsonAttendanceRepository>();
        services.AddScoped<ILearningTimeRepository, JsonLearningTimeRepository>();
        services.AddScoped<IPortfolioRepository, JsonPortfolioRepository>();
        services.AddScoped<ICurriculumResourceRepository, JsonCurriculumResourceRepository>();
        services.AddScoped<IStudentCurriculumRepository, JsonStudentCurriculumRepository>();
        services.AddScoped<IParentNoteRepository, JsonParentNoteRepository>();
        services.AddScoped<IYearbookRepository, JsonYearbookRepository>();
        services.AddScoped<IPortfolioFileStorage, PortfolioFileStorage>();

        return services;
    }
}
