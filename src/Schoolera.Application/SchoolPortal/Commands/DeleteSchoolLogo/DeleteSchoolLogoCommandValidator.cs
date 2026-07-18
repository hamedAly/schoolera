using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeleteSchoolLogo;

public sealed class DeleteSchoolLogoCommandValidator : AbstractValidator<DeleteSchoolLogoCommand>
{
    public DeleteSchoolLogoCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
