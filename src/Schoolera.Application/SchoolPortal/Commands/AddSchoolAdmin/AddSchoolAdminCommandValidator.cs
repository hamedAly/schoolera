using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.AddSchoolAdmin;

public sealed class AddSchoolAdminCommandValidator : AbstractValidator<AddSchoolAdminCommand>
{
    public AddSchoolAdminCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
