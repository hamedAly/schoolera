using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolFeeInstallmentDisplay;

public sealed class CreateSchoolFeeInstallmentDisplayCommandValidator
    : AbstractValidator<CreateSchoolFeeInstallmentDisplayCommand>
{
    public CreateSchoolFeeInstallmentDisplayCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.FeeId).NotEmpty();
        RuleFor(command => command.Body.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Body.SequenceNumber).GreaterThan(0);
    }
}
