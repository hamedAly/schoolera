using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.RequestParentAppointmentReschedule;

public sealed class RequestParentAppointmentRescheduleCommandValidator
    : AbstractValidator<RequestParentAppointmentRescheduleCommand>
{
    public RequestParentAppointmentRescheduleCommandValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Body.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body.RowVersion).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
