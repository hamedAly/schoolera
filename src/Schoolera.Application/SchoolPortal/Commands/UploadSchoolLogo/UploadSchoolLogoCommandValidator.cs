using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UploadSchoolLogo;

public sealed class UploadSchoolLogoCommandValidator : AbstractValidator<UploadSchoolLogoCommand>
{
    public UploadSchoolLogoCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
