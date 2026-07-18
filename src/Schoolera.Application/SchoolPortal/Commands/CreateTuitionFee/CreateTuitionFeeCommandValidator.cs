using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.CreateTuitionFee;

public sealed class CreateTuitionFeeCommandValidator : AbstractValidator<CreateTuitionFeeCommand>
{
    public CreateTuitionFeeCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
