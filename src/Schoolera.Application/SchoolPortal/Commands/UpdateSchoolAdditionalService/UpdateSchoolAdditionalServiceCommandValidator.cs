using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolAdditionalService;

public sealed class UpdateSchoolAdditionalServiceCommandValidator : AbstractValidator<UpdateSchoolAdditionalServiceCommand>
{
    public UpdateSchoolAdditionalServiceCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
