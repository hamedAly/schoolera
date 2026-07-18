using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.PublishTuitionFee;

public sealed class PublishTuitionFeeCommandValidator : AbstractValidator<PublishTuitionFeeCommand>
{
    public PublishTuitionFeeCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.FeeId).NotEmpty();
    }
}
