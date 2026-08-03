using FluentValidation;
using SmartHealthcare.Application.Common.Interfaces;
using SmartHealthcare.Application.DTOs;
using SmartHealthcare.Application.Repositories;
using SmartHealthcare.Domain.Entities;

namespace SmartHealthcare.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static void MapAppointmentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/appointments").WithTags("Appointments").RequireAuthorization();

        // 1. GetAll (with pagination, filtering, sorting)
        group.MapGet("/", async (
            Guid? doctorId,
            Guid? patientId,
            string? status,
            DateTime? fromDateUtc,
            DateTime? toDateUtc,
            int page,
            int pageSize,
            string? sortBy,
            string? sortOrder,
            IAppointmentRepository appointmentRepo,
            CancellationToken ct) =>
        {
            int pageNumber = page > 0 ? page : 1;
            int size = pageSize > 0 ? pageSize : 10;

            var (items, totalCount) = await appointmentRepo.GetPagedAppointmentsAsync(
                doctorId,
                patientId,
                status,
                fromDateUtc,
                toDateUtc,
                pageNumber,
                size,
                sortBy ?? "StartUtc",
                sortOrder ?? "asc",
                ct);

            var dtos = items.Select(a => new AppointmentResponse(
                a.Id,
                a.PatientId,
                a.Patient?.User is not null ? $"{a.Patient.User.FirstName} {a.Patient.User.LastName}" : "Unknown Patient",
                a.DoctorId,
                a.Doctor?.User is not null ? $"Dr. {a.Doctor.User.FirstName} {a.Doctor.User.LastName}" : "Unknown Doctor",
                a.AppointmentStartUtc,
                a.AppointmentEndUtc,
                a.Status.ToString(),
                a.ReasonForVisit,
                a.CancellationReason
            )).ToList();

            var pagedResult = new PagedResult<AppointmentResponse>(dtos, totalCount, pageNumber, size);
            return Results.Ok(pagedResult);
        }).WithName("GetAllAppointments");

        // 2. GetById
        group.MapGet("/{id:guid}", async (
            Guid id,
            IAppointmentRepository appointmentRepo,
            CancellationToken ct) =>
        {
            var appointment = await appointmentRepo.GetWithDetailsAsync(id, ct);
            if (appointment is null)
            {
                return Results.NotFound(new { message = $"Appointment with ID '{id}' was not found." });
            }

            var dto = new AppointmentResponse(
                appointment.Id,
                appointment.PatientId,
                appointment.Patient?.User is not null ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}" : "Unknown Patient",
                appointment.DoctorId,
                appointment.Doctor?.User is not null ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" : "Unknown Doctor",
                appointment.AppointmentStartUtc,
                appointment.AppointmentEndUtc,
                appointment.Status.ToString(),
                appointment.ReasonForVisit,
                appointment.CancellationReason
            );

            return Results.Ok(dto);
        }).WithName("GetAppointmentById");

        // 3. Book Appointment
        group.MapPost("/book", async (
            BookAppointmentRequest request,
            IValidator<BookAppointmentRequest> validator,
            IAppointmentRepository appointmentRepo,
            IPatientRepository patientRepo,
            IDoctorRepository doctorRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var patient = await patientRepo.GetByIdAsync(request.PatientId, ct);
            if (patient is null)
            {
                return Results.NotFound(new { message = $"Patient with ID '{request.PatientId}' was not found." });
            }

            var doctor = await doctorRepo.GetByIdAsync(request.DoctorId, ct);
            if (doctor is null)
            {
                return Results.NotFound(new { message = $"Doctor with ID '{request.DoctorId}' was not found." });
            }

            // Check for existing overlapping appointments for doctor
            var existingAppointments = await appointmentRepo.GetDoctorAppointmentsForDateAsync(request.DoctorId, request.StartTimeUtc, ct);
            bool isOverlapping = existingAppointments.Any(a =>
                a.AppointmentStartUtc < request.EndTimeUtc && request.StartTimeUtc < a.AppointmentEndUtc);

            if (isOverlapping)
            {
                return Results.Conflict(new { message = "The selected doctor already has an appointment scheduled at this time." });
            }

            var appointment = new Appointment(
                request.PatientId,
                request.DoctorId,
                request.StartTimeUtc,
                request.EndTimeUtc,
                request.ReasonForVisit
            );

            await appointmentRepo.AddAsync(appointment, ct);
            await unitOfWork.SaveChangesAsync(ct);

            var response = new AppointmentResponse(
                appointment.Id,
                appointment.PatientId,
                patient.User is not null ? $"{patient.User.FirstName} {patient.User.LastName}" : "Patient",
                appointment.DoctorId,
                doctor.User is not null ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}" : "Doctor",
                appointment.AppointmentStartUtc,
                appointment.AppointmentEndUtc,
                appointment.Status.ToString(),
                appointment.ReasonForVisit,
                appointment.CancellationReason
            );

            return Results.Created($"/api/v1/appointments/{appointment.Id}", response);
        }).WithName("BookAppointment");

        // 4. Cancel Appointment
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            CancelAppointmentRequest request,
            IValidator<CancelAppointmentRequest> validator,
            IAppointmentRepository appointmentRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var appointment = await appointmentRepo.GetByIdAsync(id, ct);
            if (appointment is null)
            {
                return Results.NotFound(new { message = $"Appointment with ID '{id}' was not found." });
            }

            appointment.Cancel(request.Reason);
            appointmentRepo.Update(appointment);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Appointment cancelled successfully.", appointmentId = id });
        }).WithName("CancelAppointment");

        // 5. Complete Appointment
        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            CompleteAppointmentRequest? request,
            IValidator<CompleteAppointmentRequest> validator,
            IAppointmentRepository appointmentRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            if (request is not null)
            {
                var validationResult = await validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }
            }

            var appointment = await appointmentRepo.GetByIdAsync(id, ct);
            if (appointment is null)
            {
                return Results.NotFound(new { message = $"Appointment with ID '{id}' was not found." });
            }

            appointment.Complete();
            appointmentRepo.Update(appointment);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Appointment completed successfully.", appointmentId = id });
        }).WithName("CompleteAppointment");
    }
}
