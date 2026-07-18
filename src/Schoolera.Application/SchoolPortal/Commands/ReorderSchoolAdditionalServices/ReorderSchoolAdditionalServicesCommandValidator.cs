using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ReorderSchoolAdditionalServices;

public sealed class ReorderSchoolAdditionalServicesCommandValidator : AbstractValidator<ReorderSchoolAdditionalServicesCommand>
{
    public ReorderSchoolAdditionalServicesCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
