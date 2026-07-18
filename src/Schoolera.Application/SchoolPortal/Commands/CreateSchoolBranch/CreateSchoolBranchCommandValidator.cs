using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolBranch;

public sealed class CreateSchoolBranchCommandValidator : AbstractValidator<CreateSchoolBranchCommand>
{
    public CreateSchoolBranchCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
