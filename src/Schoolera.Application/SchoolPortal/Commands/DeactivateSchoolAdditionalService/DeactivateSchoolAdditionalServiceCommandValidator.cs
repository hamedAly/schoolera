using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolAdditionalService;

public sealed class DeactivateSchoolAdditionalServiceCommandValidator : AbstractValidator<DeactivateSchoolAdditionalServiceCommand>
{
    public DeactivateSchoolAdditionalServiceCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
