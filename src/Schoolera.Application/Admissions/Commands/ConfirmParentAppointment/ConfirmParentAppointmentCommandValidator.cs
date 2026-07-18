using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.ConfirmParentAppointment;

public sealed class ConfirmParentAppointmentCommandValidator
    : AbstractValidator<ConfirmParentAppointmentCommand>
{
    public ConfirmParentAppointmentCommandValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Body.RowVersion).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
