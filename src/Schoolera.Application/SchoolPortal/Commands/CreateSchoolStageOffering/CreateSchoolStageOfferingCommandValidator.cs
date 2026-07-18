using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolStageOffering;

public sealed class CreateSchoolStageOfferingCommandValidator : AbstractValidator<CreateSchoolStageOfferingCommand>
{
    public CreateSchoolStageOfferingCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
