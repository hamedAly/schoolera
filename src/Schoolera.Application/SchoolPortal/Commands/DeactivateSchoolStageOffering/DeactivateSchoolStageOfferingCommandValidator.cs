using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolStageOffering;

public sealed class DeactivateSchoolStageOfferingCommandValidator : AbstractValidator<DeactivateSchoolStageOfferingCommand>
{
    public DeactivateSchoolStageOfferingCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
