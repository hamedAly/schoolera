using FluentValidation;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolTeamMember;

public sealed class UpdateSchoolTeamMemberCommandValidator : AbstractValidator<UpdateSchoolTeamMemberCommand>
{
    public UpdateSchoolTeamMemberCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.MembershipId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Role).IsInEnum()
            .Must(role => role is >= SchoolTeamRole.SchoolAdmin and <= SchoolTeamRole.ContentModerator);
        RuleFor(command => command.Body.BranchScopeMode).IsInEnum();
    }
}
