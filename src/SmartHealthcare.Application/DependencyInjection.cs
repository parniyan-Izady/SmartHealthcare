using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using SmartHealthcare.Application.Services;

namespace SmartHealthcare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
