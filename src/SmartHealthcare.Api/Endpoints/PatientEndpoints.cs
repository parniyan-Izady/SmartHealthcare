using SmartHealthcare.Application.DTOs;
using SmartHealthcare.Application.Services;

namespace SmartHealthcare.Api.Endpoints;

public static class PatientEndpoints
{
    public static void MapPatientEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/patients").WithTags("Patients").RequireAuthorization();

        group.MapGet("/reports/ado", async (IPatientReportService reportService, CancellationToken ct) =>
        {
            var report = await reportService.GetHighPerformancePatientReportAsync(ct);
            return Results.Ok(report);
        }).WithName("GetAdoPatientReport");
    }
}
