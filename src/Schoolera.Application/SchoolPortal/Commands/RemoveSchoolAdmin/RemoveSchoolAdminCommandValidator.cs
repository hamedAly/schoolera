using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.RemoveSchoolAdmin;

public sealed class RemoveSchoolAdminCommandValidator : AbstractValidator<RemoveSchoolAdminCommand>
{
    public RemoveSchoolAdminCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
