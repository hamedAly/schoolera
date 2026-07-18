using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolFeeVisibility;

public sealed class UpdateSchoolFeeVisibilityCommandValidator
    : AbstractValidator<UpdateSchoolFeeVisibilityCommand>
{
    public UpdateSchoolFeeVisibilityCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
