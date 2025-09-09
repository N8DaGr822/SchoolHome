using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using HomeschoolManager.Infrastructure.Data;
using HomeschoolManager.Infrastructure.Repositories;
using HomeschoolManager.Core.Interfaces;

namespace HomeschoolManager.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure SQLite database
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=homeschool.db";
            
        services.AddDbContext<HomeschoolDbContext>(options =>
            options.UseSqlite(connectionString));
        
        // Register repositories
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<ILessonPlanRepository, LessonPlanRepository>();
        
        return services;
    }
} 