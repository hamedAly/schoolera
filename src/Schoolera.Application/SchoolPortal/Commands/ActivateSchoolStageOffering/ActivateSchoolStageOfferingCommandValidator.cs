using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ActivateSchoolStageOffering;

public sealed class ActivateSchoolStageOfferingCommandValidator : AbstractValidator<ActivateSchoolStageOfferingCommand>
{
    public ActivateSchoolStageOfferingCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
