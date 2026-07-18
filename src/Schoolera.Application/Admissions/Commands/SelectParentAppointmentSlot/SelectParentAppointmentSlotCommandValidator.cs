using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.SelectParentAppointmentSlot;

public sealed class SelectParentAppointmentSlotCommandValidator
    : AbstractValidator<SelectParentAppointmentSlotCommand>
{
    public SelectParentAppointmentSlotCommandValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Body.SlotId).NotEmpty();
        RuleFor(x => x.Body.RowVersion).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
