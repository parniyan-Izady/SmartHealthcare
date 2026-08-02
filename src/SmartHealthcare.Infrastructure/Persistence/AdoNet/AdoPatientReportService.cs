using System.Data;
using SmartHealthcare.Application.Common.Interfaces;
using SmartHealthcare.Application.DTOs;
using SmartHealthcare.Application.Services;

namespace SmartHealthcare.Infrastructure.Persistence.AdoNet;

/// <summary>
/// CQRS-style Read Model Optimization using raw ADO.NET.
/// Architecture Note: EF Core is used for transactional operations and entity management,
/// while ADO.NET is used for high-performance read models and reporting queries.
/// </summary>
public class AdoPatientReportService : IPatientReportService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public AdoPatientReportService(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<IReadOnlyList<PatientReportDto>> GetHighPerformancePatientReportAsync(CancellationToken cancellationToken = default)
    {
        var reports = new List<PatientReportDto>();

        using var connection = _sqlConnectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT 
                p.Id AS PatientId,
                u.FirstName + ' ' + u.LastName AS FullName,
                p.NationalCode,
                p.PhoneNumber,
                COUNT(a.Id) AS TotalAppointmentsCount,
                MAX(a.AppointmentStartUtc) AS LastAppointmentDateUtc
            FROM Patients p
            INNER JOIN Users u ON p.UserId = u.Id
            LEFT JOIN Appointments a ON a.PatientId = p.Id
            WHERE p.IsDeleted = 0
            GROUP BY p.Id, u.FirstName, u.LastName, p.NationalCode, p.PhoneNumber
            ORDER BY TotalAppointmentsCount DESC";

        if (connection.State != ConnectionState.Open)
            connection.Open();

        using var reader = await ((Microsoft.Data.SqlClient.SqlCommand)command).ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            reports.Add(new PatientReportDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.IsDBNull(5) ? null : reader.GetDateTime(5)
            ));
        }

        return reports;
    }
}
