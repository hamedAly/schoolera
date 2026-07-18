using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ActivateTuitionFee;

public sealed class ActivateTuitionFeeCommandValidator : AbstractValidator<ActivateTuitionFeeCommand>
{
    public ActivateTuitionFeeCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
