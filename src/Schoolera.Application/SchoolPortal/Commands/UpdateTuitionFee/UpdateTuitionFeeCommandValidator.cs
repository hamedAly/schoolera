using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateTuitionFee;

public sealed class UpdateTuitionFeeCommandValidator : AbstractValidator<UpdateTuitionFeeCommand>
{
    public UpdateTuitionFeeCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
