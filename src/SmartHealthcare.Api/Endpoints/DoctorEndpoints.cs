using FluentValidation;
using SmartHealthcare.Application.Common.Interfaces;
using SmartHealthcare.Application.DTOs;
using SmartHealthcare.Application.Repositories;
using SmartHealthcare.Domain.Entities;
using SmartHealthcare.Domain.Enums;

namespace SmartHealthcare.Api.Endpoints;

public static class DoctorEndpoints
{
    public static void MapDoctorEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/doctors").WithTags("Doctors");

        // 1. GetAll (with pagination, filtering, sorting)
        group.MapGet("/", async (
            string? specialty,
            string? searchTerm,
            bool? isActive,
            int page,
            int pageSize,
            string? sortBy,
            string? sortOrder,
            IDoctorRepository doctorRepository,
            CancellationToken ct) =>
        {
            int pageNumber = page > 0 ? page : 1;
            int size = pageSize > 0 ? pageSize : 10;

            var (items, totalCount) = await doctorRepository.GetPagedDoctorsAsync(
                specialty,
                searchTerm,
                isActive,
                pageNumber,
                size,
                sortBy ?? "LastName",
                sortOrder ?? "asc",
                ct);

            var dtos = items.Select(d => new DoctorResponse(
                d.Id,
                d.UserId,
                $"{d.User.FirstName} {d.User.LastName}",
                d.User.Email,
                d.MedicalLicenseNumber,
                d.MedicalSpecialty,
                d.ConsultationFee,
                d.OfficeAddress,
                d.User.IsActive
            )).ToList();

            var pagedResult = new PagedResult<DoctorResponse>(dtos, totalCount, pageNumber, size);
            return Results.Ok(pagedResult);
        }).WithName("GetAllDoctors");

        // 2. SearchBySpecialty
        group.MapGet("/search", async (
            string specialty,
            IDoctorRepository doctorRepository,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(specialty))
            {
                return Results.BadRequest(new { message = "Specialty search parameter is required." });
            }

            var doctors = await doctorRepository.SearchBySpecialtyAsync(specialty, ct);
            var dtos = doctors.Select(d => new DoctorResponse(
                d.Id,
                d.UserId,
                $"{d.User.FirstName} {d.User.LastName}",
                d.User.Email,
                d.MedicalLicenseNumber,
                d.MedicalSpecialty,
                d.ConsultationFee,
                d.OfficeAddress,
                d.User.IsActive
            )).ToList();

            return Results.Ok(dtos);
        }).WithName("SearchDoctorsBySpecialty");

        // 3. GetById
        group.MapGet("/{id:guid}", async (
            Guid id,
            IDoctorRepository doctorRepository,
            CancellationToken ct) =>
        {
            var doctor = await doctorRepository.GetWithDetailsAsync(id, ct);
            if (doctor is null)
            {
                return Results.NotFound(new { message = $"Doctor with ID '{id}' was not found." });
            }

            var dto = new DoctorResponse(
                doctor.Id,
                doctor.UserId,
                $"{doctor.User.FirstName} {doctor.User.LastName}",
                doctor.User.Email,
                doctor.MedicalLicenseNumber,
                doctor.MedicalSpecialty,
                doctor.ConsultationFee,
                doctor.OfficeAddress,
                doctor.User.IsActive
            );

            return Results.Ok(dto);
        }).WithName("GetDoctorById");

        // 4. Create
        group.MapPost("/", async (
            CreateDoctorRequest request,
            IValidator<CreateDoctorRequest> validator,
            IDoctorRepository doctorRepository,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var existingUser = await userRepository.GetByEmailAsync(request.Email, ct);
            if (existingUser is not null)
            {
                return Results.Conflict(new { message = "User with this email already exists." });
            }

            var existingDoctor = await doctorRepository.GetByLicenseNumberAsync(request.MedicalLicenseNumber, ct);
            if (existingDoctor is not null)
            {
                return Results.Conflict(new { message = "Doctor with this medical license number already exists." });
            }

            string passwordHash = passwordHasher.HashPassword(request.Password);
            var user = new User(request.FirstName, request.LastName, request.Email, passwordHash, UserRole.Doctor);
            await userRepository.AddAsync(user, ct);

            var doctor = new Doctor(user.Id, request.MedicalLicenseNumber, request.MedicalSpecialty, request.ConsultationFee, request.OfficeAddress);
            await doctorRepository.AddAsync(doctor, ct);

            await unitOfWork.SaveChangesAsync(ct);

            var response = new DoctorResponse(
                doctor.Id,
                user.Id,
                $"{user.FirstName} {user.LastName}",
                user.Email,
                doctor.MedicalLicenseNumber,
                doctor.MedicalSpecialty,
                doctor.ConsultationFee,
                doctor.OfficeAddress,
                user.IsActive
            );

            return Results.Created($"/api/v1/doctors/{doctor.Id}", response);
        }).WithName("CreateDoctor").RequireAuthorization();

        // 5. Update
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateDoctorRequest request,
            IValidator<UpdateDoctorRequest> validator,
            IDoctorRepository doctorRepository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var doctor = await doctorRepository.GetWithDetailsAsync(id, ct);
            if (doctor is null)
            {
                return Results.NotFound(new { message = $"Doctor with ID '{id}' was not found." });
            }

            // Update Doctor properties
            typeof(Doctor).GetProperty(nameof(Doctor.MedicalSpecialty))?.SetValue(doctor, request.MedicalSpecialty);
            typeof(Doctor).GetProperty(nameof(Doctor.ConsultationFee))?.SetValue(doctor, request.ConsultationFee);
            typeof(Doctor).GetProperty(nameof(Doctor.OfficeAddress))?.SetValue(doctor, request.OfficeAddress);
            doctor.MarkUpdated();

            doctorRepository.Update(doctor);
            await unitOfWork.SaveChangesAsync(ct);

            var response = new DoctorResponse(
                doctor.Id,
                doctor.UserId,
                $"{request.FirstName} {request.LastName}",
                doctor.User.Email,
                doctor.MedicalLicenseNumber,
                doctor.MedicalSpecialty,
                doctor.ConsultationFee,
                doctor.OfficeAddress,
                doctor.User.IsActive
            );

            return Results.Ok(response);
        }).WithName("UpdateDoctor").RequireAuthorization();

        // 6. Delete (Soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IDoctorRepository doctorRepository,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var doctor = await doctorRepository.GetByIdAsync(id, ct);
            if (doctor is null)
            {
                return Results.NotFound(new { message = $"Doctor with ID '{id}' was not found." });
            }

            doctor.SoftDelete();
            doctorRepository.Update(doctor);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.NoContent();
        }).WithName("DeleteDoctor").RequireAuthorization();
    }
}
