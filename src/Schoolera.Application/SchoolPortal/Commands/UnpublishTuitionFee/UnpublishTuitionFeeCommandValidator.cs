using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UnpublishTuitionFee;

public sealed class UnpublishTuitionFeeCommandValidator : AbstractValidator<UnpublishTuitionFeeCommand>
{
    public UnpublishTuitionFeeCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.FeeId).NotEmpty();
    }
}
