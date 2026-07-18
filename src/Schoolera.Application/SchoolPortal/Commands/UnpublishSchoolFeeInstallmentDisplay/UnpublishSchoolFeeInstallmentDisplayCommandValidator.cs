using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolFeeInstallmentDisplay;

public sealed class UnpublishSchoolFeeInstallmentDisplayCommandValidator : AbstractValidator<UnpublishSchoolFeeInstallmentDisplayCommand>
{
    public UnpublishSchoolFeeInstallmentDisplayCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.FeeId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
