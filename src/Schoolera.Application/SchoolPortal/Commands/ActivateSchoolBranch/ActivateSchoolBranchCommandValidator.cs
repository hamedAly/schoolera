using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ActivateSchoolBranch;

public sealed class ActivateSchoolBranchCommandValidator : AbstractValidator<ActivateSchoolBranchCommand>
{
    public ActivateSchoolBranchCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
