using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolBranch;

public sealed class DeactivateSchoolBranchCommandValidator : AbstractValidator<DeactivateSchoolBranchCommand>
{
    public DeactivateSchoolBranchCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
