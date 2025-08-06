using Microsoft.Extensions.DependencyInjection;
using HomeschoolManager.Application.Services;

namespace HomeschoolManager.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services here
        services.AddScoped<IStudentService, StudentService>();
        // Add other services as they are implemented
        
        return services;
    }
} 