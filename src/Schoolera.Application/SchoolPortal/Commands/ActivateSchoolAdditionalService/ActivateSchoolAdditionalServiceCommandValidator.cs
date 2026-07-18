using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ActivateSchoolAdditionalService;

public sealed class ActivateSchoolAdditionalServiceCommandValidator : AbstractValidator<ActivateSchoolAdditionalServiceCommand>
{
    public ActivateSchoolAdditionalServiceCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
