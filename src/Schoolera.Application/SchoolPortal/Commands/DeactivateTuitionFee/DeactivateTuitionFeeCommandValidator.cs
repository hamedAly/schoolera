using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateTuitionFee;

public sealed class DeactivateTuitionFeeCommandValidator : AbstractValidator<DeactivateTuitionFeeCommand>
{
    public DeactivateTuitionFeeCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
