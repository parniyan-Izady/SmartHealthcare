using FluentValidation;
using SmartHealthcare.Application.DTOs;

namespace SmartHealthcare.Application.Validators;

public class CreateDoctorRequestValidator : AbstractValidator<CreateDoctorRequest>
{
    public CreateDoctorRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.MedicalLicenseNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.MedicalSpecialty).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ConsultationFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OfficeAddress).NotEmpty().MaximumLength(200);
    }
}

public class UpdateDoctorRequestValidator : AbstractValidator<UpdateDoctorRequest>
{
    public UpdateDoctorRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MedicalSpecialty).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ConsultationFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OfficeAddress).NotEmpty().MaximumLength(200);
    }
}
