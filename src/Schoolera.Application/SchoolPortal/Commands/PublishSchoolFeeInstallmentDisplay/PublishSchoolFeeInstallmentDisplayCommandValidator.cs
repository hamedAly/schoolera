using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.PublishSchoolFeeInstallmentDisplay;

public sealed class PublishSchoolFeeInstallmentDisplayCommandValidator : AbstractValidator<PublishSchoolFeeInstallmentDisplayCommand>
{
    public PublishSchoolFeeInstallmentDisplayCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.FeeId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
