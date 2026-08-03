using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SmartHealthcare.Api.Endpoints;
using SmartHealthcare.Api.Middlewares;
using SmartHealthcare.Application;
using SmartHealthcare.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Services to Container
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "SuperSecretKeyForSmartHealthcareCleanArchitecture2026!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "SmartHealthcare",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "SmartHealthcareClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// OpenAPI & Scalar Setup
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure HTTP Request Pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "SmartHealthcare OpenAPI Reference";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Minimal API Endpoint Mapping
app.MapAuthEndpoints();
app.MapPatientEndpoints();

app.Run();
