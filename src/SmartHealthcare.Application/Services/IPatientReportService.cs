using SmartHealthcare.Application.DTOs;

namespace SmartHealthcare.Application.Services;

public interface IPatientReportService
{
    Task<IReadOnlyList<PatientReportDto>> GetHighPerformancePatientReportAsync(CancellationToken cancellationToken = default);
}
