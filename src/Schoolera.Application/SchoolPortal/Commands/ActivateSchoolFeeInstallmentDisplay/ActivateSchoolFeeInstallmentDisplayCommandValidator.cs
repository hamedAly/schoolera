using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ActivateSchoolFeeInstallmentDisplay;

public sealed class ActivateSchoolFeeInstallmentDisplayCommandValidator : AbstractValidator<ActivateSchoolFeeInstallmentDisplayCommand>
{
    public ActivateSchoolFeeInstallmentDisplayCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.FeeId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
