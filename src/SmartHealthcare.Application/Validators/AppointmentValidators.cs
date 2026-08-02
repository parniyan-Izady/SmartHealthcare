using FluentValidation;
using SmartHealthcare.Application.DTOs;

namespace SmartHealthcare.Application.Validators;

public class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.StartTimeUtc).NotEmpty().GreaterThan(DateTime.UtcNow.AddMinutes(-5));
        RuleFor(x => x.EndTimeUtc).NotEmpty().GreaterThan(x => x.StartTimeUtc);
        RuleFor(x => x.ReasonForVisit).MaximumLength(500);
    }
}

public class CancelAppointmentRequestValidator : AbstractValidator<CancelAppointmentRequest>
{
    public CancelAppointmentRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(250);
    }
}

public class CompleteAppointmentRequestValidator : AbstractValidator<CompleteAppointmentRequest>
{
    public CompleteAppointmentRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
