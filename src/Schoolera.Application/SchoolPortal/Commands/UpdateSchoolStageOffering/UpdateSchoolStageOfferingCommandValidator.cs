using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolStageOffering;

public sealed class UpdateSchoolStageOfferingCommandValidator : AbstractValidator<UpdateSchoolStageOfferingCommand>
{
    public UpdateSchoolStageOfferingCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
