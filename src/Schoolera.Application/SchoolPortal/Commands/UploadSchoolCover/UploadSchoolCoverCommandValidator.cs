using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UploadSchoolCover;

public sealed class UploadSchoolCoverCommandValidator : AbstractValidator<UploadSchoolCoverCommand>
{
    public UploadSchoolCoverCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
