using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartHealthcare.Application.Common.Interfaces;
using SmartHealthcare.Application.Repositories;
using SmartHealthcare.Application.Services;
using SmartHealthcare.Infrastructure.Persistence.AdoNet;
using SmartHealthcare.Infrastructure.Persistence.DbContext;
using SmartHealthcare.Infrastructure.Persistence.Repositories;
using SmartHealthcare.Infrastructure.Security;

namespace SmartHealthcare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Database=SmartHealthcareDb;Trusted_Connection=True;TrustServerCertificate=True;";

        // EF Core Registration
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Repositories & Unit Of Work
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();

        // ADO.NET Registration (CQRS Read Model Optimization)
        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<IPatientReportService, AdoPatientReportService>();

        // Security & JWT
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
