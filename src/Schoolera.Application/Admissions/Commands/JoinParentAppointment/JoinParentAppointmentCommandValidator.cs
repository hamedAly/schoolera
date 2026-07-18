using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.JoinParentAppointment;

public sealed class JoinParentAppointmentCommandValidator
    : AbstractValidator<JoinParentAppointmentCommand>
{
    public JoinParentAppointmentCommandValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Body.RowVersion).NotEmpty();
    }
}
