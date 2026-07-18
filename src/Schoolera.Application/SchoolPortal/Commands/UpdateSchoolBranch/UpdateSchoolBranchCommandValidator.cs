using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolBranch;

public sealed class UpdateSchoolBranchCommandValidator : AbstractValidator<UpdateSchoolBranchCommand>
{
    public UpdateSchoolBranchCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
