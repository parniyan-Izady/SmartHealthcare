using FluentValidation;
using SmartHealthcare.Application.DTOs;

namespace SmartHealthcare.Application.Validators;

public class CreateMedicalRecordRequestValidator : AbstractValidator<CreateMedicalRecordRequest>
{
    public CreateMedicalRecordRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.Diagnosis).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrescriptionNotes).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.TreatmentPlan).MaximumLength(1000);
    }
}

public class UpdateMedicalRecordRequestValidator : AbstractValidator<UpdateMedicalRecordRequest>
{
    public UpdateMedicalRecordRequestValidator()
    {
        RuleFor(x => x.Diagnosis).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrescriptionNotes).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.TreatmentPlan).MaximumLength(1000);
    }
}
