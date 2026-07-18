using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolFeeInstallmentDisplay;

public sealed class UpdateSchoolFeeInstallmentDisplayCommandValidator
    : AbstractValidator<UpdateSchoolFeeInstallmentDisplayCommand>
{
    public UpdateSchoolFeeInstallmentDisplayCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.FeeId).NotEmpty();
        RuleFor(command => command.InstallmentId).NotEmpty();
        RuleFor(command => command.Body.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Body.SequenceNumber).GreaterThan(0);
    }
}
