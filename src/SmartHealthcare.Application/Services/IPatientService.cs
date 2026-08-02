using SmartHealthcare.Application.DTOs;

namespace SmartHealthcare.Application.Services;

public interface IPatientService
{
    Task<PatientResponse> CreatePatientAsync(CreatePatientRequest request, CancellationToken cancellationToken = default);
    Task<PatientResponse?> GetPatientByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientResponse>> GetAllPatientsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientReportDto>> GetAdoPatientReportAsync(CancellationToken cancellationToken = default);
}
