using SmartHealthcare.Application.DTOs;
using SmartHealthcare.Application.Services;

namespace SmartHealthcare.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/register", async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.RegisterAsync(request, ct);
            return Results.Ok(result);
        }).WithName("RegisterUser");

        group.MapPost("/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.LoginAsync(request, ct);
            return Results.Ok(result);
        }).WithName("LoginUser");
    }
}
