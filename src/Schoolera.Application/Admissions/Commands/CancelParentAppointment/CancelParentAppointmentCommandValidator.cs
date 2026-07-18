using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.CancelParentAppointment;

public sealed class CancelParentAppointmentCommandValidator
    : AbstractValidator<CancelParentAppointmentCommand>
{
    public CancelParentAppointmentCommandValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Body.Reason).MaximumLength(500);
        RuleFor(x => x.Body.RowVersion).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
