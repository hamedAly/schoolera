using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeleteSchoolCover;

public sealed class DeleteSchoolCoverCommandValidator : AbstractValidator<DeleteSchoolCoverCommand>
{
    public DeleteSchoolCoverCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
