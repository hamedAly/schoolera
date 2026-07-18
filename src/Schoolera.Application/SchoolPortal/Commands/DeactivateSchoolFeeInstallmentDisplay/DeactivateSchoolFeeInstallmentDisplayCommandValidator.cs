using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolFeeInstallmentDisplay;

public sealed class DeactivateSchoolFeeInstallmentDisplayCommandValidator : AbstractValidator<DeactivateSchoolFeeInstallmentDisplayCommand>
{
    public DeactivateSchoolFeeInstallmentDisplayCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.FeeId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
