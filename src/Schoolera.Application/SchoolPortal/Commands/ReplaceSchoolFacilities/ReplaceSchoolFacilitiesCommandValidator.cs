using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ReplaceSchoolFacilities;

public sealed class ReplaceSchoolFacilitiesCommandValidator : AbstractValidator<ReplaceSchoolFacilitiesCommand>
{
    public ReplaceSchoolFacilitiesCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
