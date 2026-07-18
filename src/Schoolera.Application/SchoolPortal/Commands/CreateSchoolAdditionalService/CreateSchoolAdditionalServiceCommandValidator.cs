using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolAdditionalService;

public sealed class CreateSchoolAdditionalServiceCommandValidator : AbstractValidator<CreateSchoolAdditionalServiceCommand>
{
    public CreateSchoolAdditionalServiceCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
